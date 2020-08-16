namespace NodeController {
    using System;
    using UnityEngine;
    using KianCommons;
    using CSURUtil = Util.CSURUtil;
    using TernaryBool = CSUtil.Commons.TernaryBool;
    using Log = KianCommons.Log;
    using ColossalFramework.Math;
    using KianCommons.Math;

    [Serializable]
    public class SegmentEndData {
        // intrinsic
        public ushort NodeID;
        public ushort SegmentID;
        public bool IsStartNode => NetUtil.IsStartNode(segmentId: SegmentID, nodeId: NodeID);

        public override string ToString() {
            return GetType().Name + $"(segment:{SegmentID} node:{NodeID})";
        }

        /// <summary>clone</summary>
        public SegmentEndData(SegmentEndData template) =>
            HelpersExtensions.CopyProperties(this, template);

        public SegmentEndData Clone() => new SegmentEndData(this);

        // defaults
        public float DefaultCornerOffset => CSURUtil.GetMinCornerOffset(NodeID);
        public bool DefaultFlatJunctions => NodeID.ToNode().Info.m_flatJunctions;
        public NetSegment.Flags DefaultFlags;

        // cache
        public bool HasPedestrianLanes;
        public float CurveRaduis0;
        public int PedestrianLaneCount;
        public Vector3 CachedLeftCornerPos, CachedLeftCornerDir, CachedRightCornerPos, CachedRightCornerDir;// left and right is when you go away form junction
        public Vector3 LeftCornerDir0, RightCornerDir0, LeftCorner0, RightCorner0;
        Bezier3 LeftBezier, CenterBezier, RightBezier;
        float LeftBezierLength, CenterBezierLength, RightBezierLength; // left and right when going away from the node.

        // Configurable
        public float CornerOffset;
        public bool FlatJunctions;
        public bool NoCrossings;
        public bool NoMarkings;
        public bool NoJunctionTexture;
        public bool NoJunctionProps; // excluding TL
        public bool NoTLProps;
        public Vector3 DeltaLeftCornerPos, DeltaRightCornerPos;// left and right is when you go away form junction
        public Vector2 LeftCornerRoation, RightCornerRoation;

        // shortcuts
        public ref NetSegment Segment => ref SegmentID.ToSegment();
        public ref NetNode Node => ref NodeID.ToNode();
        public NodeData NodeData => NodeManager.Instance.buffer[NodeID];

        public SegmentEndData(ushort segmentID, ushort nodeID) {
            NodeID = nodeID;
            SegmentID = segmentID;

            Calculate();
            CornerOffset = DefaultCornerOffset;
            FlatJunctions = DefaultFlatJunctions;
        }


        public void Calculate() {
            DefaultFlags = Segment.m_flags;
            PedestrianLaneCount = Info.CountPedestrianLanes();

            // left and right is when you go away form junction
            // both in SegmentEndData Cahced* and NetSegment.CalculateCorner() 
            Segment.CalculateCorner(SegmentID, true, IsStartNode, leftSide: true,
                cornerPos: out CachedLeftCornerPos, cornerDirection: out CachedLeftCornerDir, out _);
            Segment.CalculateCorner(SegmentID, true, IsStartNode, leftSide: false,
                cornerPos: out CachedRightCornerPos, cornerDirection: out CachedRightCornerDir, out _);
            CalculateBeziers();

            Refresh();
        }
        public void CalculateBeziers() {
            float hw = Segment.Info.m_halfWidth;
            CenterBezier = SegmentID.ToSegment().CalculateSegmentBezier3(IsStartNode);
            LeftBezier = CenterBezier.CalculateParallelBezier(hw, bLeft: true);
            RightBezier = CenterBezier.CalculateParallelBezier(hw, bLeft: false);
            CenterBezierLength = CenterBezier.ArcLength();
            LeftBezierLength = LeftBezier.ArcLength();
            RightBezierLength = RightBezier.ArcLength();
        }

        public bool IsDefault() {
            bool  ret = Mathf.Abs(CornerOffset - DefaultCornerOffset) < 0.5f;
            ret &= FlatJunctions == DefaultFlatJunctions;
            ret &= NoCrossings == false;
            ret &= NoMarkings == false;
            ret &= NoJunctionTexture == false;
            ret &= NoJunctionProps == false;
            ret &= NoTLProps == false;
            ret &= DeltaRightCornerPos == Vector3.zero;
            ret &= DeltaRightCornerDir == Vector3.zero;
            ret &= DeltaLeftCornerPos == Vector3.zero;
            ret &= DeltaLeftCornerDir == Vector3.zero;

            return ret;
        }

        public void ResetToDefault() {
            CornerOffset = DefaultCornerOffset;
            FlatJunctions = DefaultFlatJunctions;
            NoCrossings = false;
            NoMarkings = false;
            NoJunctionTexture = false;
            NoJunctionProps = false;
            NoTLProps = false;
            DeltaRightCornerDir = DeltaRightCornerDir = DeltaLeftCornerPos = DeltaRightCornerPos = default;
            NetManager.instance.UpdateNode(NodeID);
        }

        public void Refresh() {
            if (!CanModifyOffset()) {
                Log.Debug("SegmentEndData.Refresh(): setting CornerOffset = DefaultCornerOffset");
                CornerOffset = DefaultCornerOffset;
            }
            if (!CanModifyFlatJunctions()) {
                FlatJunctions = DefaultFlatJunctions;
            }
            Log.Debug($"SegmentEndData.Refresh() Updating segment:{SegmentID} node:{NodeID} CornerOffset={CornerOffset}");
            if (HelpersExtensions.VERBOSE)
                Log.Debug(Environment.StackTrace);

            NetManager.instance.UpdateNode(NodeID);
        }

        bool CrossingIsRemoved() =>
            HideCrosswalks.Patches.CalculateMaterialCommons.
            ShouldHideCrossing(NodeID, SegmentID);

        public bool IsCSUR => NetUtil.IsCSUR(Info);
        public NetInfo Info => Segment.Info;
        public bool CanModifyOffset() => NodeData?.CanModifyOffset() ?? false;
        public bool CanModifyFlatJunctions() => NodeData?.CanModifyFlatJunctions()??false;

        public bool ShowClearMarkingsToggle() {
            if (IsCSUR) return false;
            if (NodeData == null) return true;
            return NodeData.NodeType == NodeTypeT.Custom;
        }

        /// <param name="leftSide">left side going away from the junction</param>
        public void ApplyCornerAdjustments(ref Vector3 cornerPos, ref Vector3 cornerDir, bool leftSide) {
            if (leftSide) {
                LeftCornerDir0 = cornerDir;
                LeftCornerPos0 = cornerPos;
            } else {
                RightCornerDir0 = cornerDir;
                RightCornerPos0 = cornerPos;
            }


            Vector3 rightwardDir = Vector3.Cross(Vector3.up, cornerDir).normalized; // going away from the junction
            Vector3 leftwardDir = -rightwardDir;
            Vector3 forwardDir = new Vector3(cornerDir.x, 0, cornerDir.z).normalized; // going away from the junction

            if (leftSide) {
                cornerPos = Transform(DeltaLeftCornerPos, LeftBezier, leftSide, out Vector3 tangent, out Vector3 normal);
                cornerDir +=  
            } else {
                deltaPos = TransformCoordinates(DeltaRightCornerPos, RightBezier, Vector3.up, leftSide);
                deltaDir = TransformCoordinates(DeltaRightCornerDir, rightwardDir, Vector3.up, forwardDir);
            }

        }

        /// <summary>
        /// tranforms input vector from relative (to x y x inputs) coordinate to absulute coodinate.
        /// </summary>
        /// <param name="v">v.x=in the direction of normal to bezier
        /// v.y=height
        /// v.z=distance on the center bezier</param>
        /// <param name="leftSide">left side going away from the junction</param>
        public Vector3 Transform(Vector3 v, Bezier3 bezier, bool leftSide, out Vector3 tangent, out Vector3 normal) {
            // z element: along the bezier.
            float t = CenterBezier.Travel(0,v.z);
            Vector3 ret = bezier.Position(t);

            // y element: height
            ret.y += v.y;

            // x element: in the direction of normakl
            bezier.NormalTangent(t, leftSide, out normal, out tangent);
            ret += normal * v.x;

            return ret;
        }

        /// <returns>if position was changed</returns>
        public bool MoveLeftCornerToAbsolutePos(Vector3 pos) {
            Vector3 rightwardDir = Vector3.Cross(Vector3.up, LeftCornerDir0).normalized; // going away from the junction
            Vector3 leftwardDir = -rightwardDir;
            Vector3 forwardDir = new Vector3(LeftCornerDir0.x, 0, LeftCornerDir0.z).normalized; // going away from the junction
            bool ret = CachedLeftCornerPos != pos;
            CachedLeftCornerPos = pos;
            DeltaLeftCornerPos = ReverseTransformCoordinats(pos-LeftCornerPos0, leftwardDir, Vector3.up, forwardDir);
            Refresh();
            return ret;
        }

        /// <bool>if position was changed</returns>
        public bool MoveRightCornerToAbsolutePos(Vector3 pos) {
            Vector3 rightwardDir = Vector3.Cross(Vector3.up, RightCornerDir0).normalized; // going away from the junction
            Vector3 leftwardDir = -rightwardDir;
            Vector3 forwardDir = new Vector3(RightCornerDir0.x, 0, RightCornerDir0.z).normalized; // going away from the junction
            bool ret = CachedRightCornerPos != pos;
            CachedRightCornerPos = pos;
            DeltaRightCornerPos = ReverseTransformCoordinats(pos-RightCornerPos0, rightwardDir, Vector3.up, forwardDir);
            Refresh();
            return ret;
        }

        public static Vector3 ReverseTransformCoordinats(Vector3 v, Bezier3 bezier) {
            Vector3 ret = default;
            ret.x = Vector3.Dot(v, x);
            ret.y = Vector3.Dot(v, y);
            ret.z = Vector3.Dot(v, z);
            return ret;
        }

        #region External Mods
        public TernaryBool ShouldHideCrossingTexture() {
            if (NodeData !=null && NodeData.NodeType == NodeTypeT.Stretch)
                return TernaryBool.False; // always ignore.
            if (NoMarkings)
                return TernaryBool.True; // always hide
            return TernaryBool.Undefined; // default.
        }
        #endregion
    }
}
