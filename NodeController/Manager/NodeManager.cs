namespace NodeController {
    using System;
    using KianCommons;
    using NodeController.Tool;

    [Serializable]
    public class NodeManager {
        #region LifeCycle
        public static NodeManager Instance { get; private set; } = new NodeManager();

        public static byte[] Serialize() => SerializationUtil.Serialize(Instance);

        public static void Deserialize(byte[] data) {
            if (data == null) {
                Instance = new NodeManager();
                Log.Debug($"NodeBlendManager.Deserialize(data=null)");

            } else {
                Log.Debug($"NodeBlendManager.Deserialize(data): data.Length={data?.Length}");
                Instance = SerializationUtil.Deserialize(data) as NodeManager;
            }
        }

        public void OnLoad() {
            RefreshAllNodes();
        }

        #endregion LifeCycle

        public NodeData[] buffer = new NodeData[NetManager.MAX_NODE_COUNT];

        #region MoveIT backward compatiblity.

        [Obsolete("delete when moveit is updated")]
        public static byte[] CopyNodeData(ushort nodeID) =>
            SerializationUtil.Serialize(Instance.buffer[nodeID])
            .LogRet($"NodeManager.CopyNodeData({nodeID}) ->");

        public static ushort TargetNodeID = 0; 

        [Obsolete("kept here for backward compatibility with MoveIT")]
        /// <param name="nodeID">target nodeID</param>
        public static void PasteNodeData(ushort nodeID, byte[] data) =>
            Instance.PasteNodeDataImp(nodeID, data);

        [Obsolete("kept here for backward compatibility with MoveIT")]
        /// <param name="nodeID">target nodeID</param>
        private void PasteNodeDataImp(ushort nodeID, byte[] data) {
            Log.Debug($"NodeManager.PasteNodeDataImp(nodeID={nodeID}, data={data})");
            if (data == null) {
                // for backward compatibality reasons its not a good idea to do this:
                // ResetNodeToDefault(nodeID); 
            } else {
                foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID))
                    SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: nodeID);
                TargetNodeID = nodeID; // must be done before deserialization.
                buffer[nodeID] = SerializationUtil.Deserialize(data) as NodeData;
                buffer[nodeID].NodeID = nodeID;
                RefreshData(nodeID);
                TargetNodeID = 0;
            }
        }
        #endregion

        public NodeData InsertNode(NetTool.ControlPoint controlPoint, NodeTypeT nodeType = NodeTypeT.Crossing) {
            if(ToolBase.ToolErrors.None != NetUtil.InsertNode(controlPoint, out ushort nodeID))
                return null;
            HelpersExtensions.Assert(nodeID!=0,"nodeID");

            foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID)) {
                var segEnd = new SegmentEndData(segmentID, nodeID);
                SegmentEndManager.Instance.SetAt(segmentID: segmentID, nodeID: nodeID, value: segEnd);
            }

            var info = controlPoint.m_segment.ToSegment().Info;
            int nPedLanes = info.CountPedestrianLanes();
            bool isRoad = info.m_netAI is RoadBaseAI;
            if (nodeType == NodeTypeT.Crossing && (nPedLanes<2 || !isRoad) )
                buffer[nodeID] = new NodeData(nodeID);
            else
                buffer[nodeID] = new NodeData(nodeID, nodeType);

            return buffer[nodeID];
        }

        public ref NodeData GetOrCreate(ushort nodeID) {
            ref NodeData data = ref Instance.buffer[nodeID];
            if (data == null) {
                data = new NodeData(nodeID);
                buffer[nodeID] = data;
            }

            foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID)) {
                SegmentEndManager.Instance.
                    GetOrCreate(segmentID: segmentID, nodeID: nodeID);
            }

            HelpersExtensions.AssertNotNull(data);
            return ref data;
        }

        /// <summary>
        /// releases data for <paramref name="nodeID"/> if uncessary. Calls update node.
        /// </summary>
        /// <param name="nodeID"></param>
        public void RefreshData(ushort nodeID) {
            if (nodeID == 0 || buffer[nodeID] == null)
                return;
            bool selected = NodeControllerTool.Instance.SelectedNodeID == nodeID;
            if (buffer[nodeID].IsDefault() && !selected) {
                ResetNodeToDefault(nodeID);
            } else {
                foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID)) {
                    var segEnd = SegmentEndManager.Instance.GetAt(segmentID: segmentID, nodeID: nodeID);
                    segEnd.Refresh();
                }
                buffer[nodeID].Refresh();
            }
        }

        public void ResetNodeToDefault(ushort nodeID) {
            if(buffer[nodeID]!=null)
                Log.Debug($"node:{nodeID} reset to defualt");
            else
                Log.Debug($"node:{nodeID} is alreadey null. no need to reset to default");

            SetNullNodeAndSegmentEnds(nodeID);

            NetManager.instance.UpdateNode(nodeID);
        }

        public void RefreshAllNodes() {
            foreach (var nodeData in buffer)
                nodeData?.Refresh();
        }

        /// <summary>Called after stock code and before postfix code.</summary>
        public void OnBeforeCalculateNode(ushort nodeID) {
            // nodeID.ToNode still has default flags.
            if (buffer[nodeID] == null)
                return;
            if (!NodeData.IsSupported(nodeID)) {
                SetNullNodeAndSegmentEnds(nodeID);
                return;
            }

            foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID)) {
                var segEnd = SegmentEndManager.Instance.
                    GetOrCreate(segmentID: segmentID, nodeID: nodeID);
                segEnd.Calculate();
            }

            buffer[nodeID].Calculate();

            if (!buffer[nodeID].CanChangeTo(buffer[nodeID].NodeType).LogRet("CanChangeTo()->")) {
                ResetNodeToDefault(nodeID);
            }
        }

        public void SetNullNodeAndSegmentEnds(ushort nodeID) {
            foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID)) {
                SegmentEndManager.Instance.
                    SetAt(segmentID: segmentID, nodeID: nodeID, value: null);
            }
            buffer[nodeID] = null;
        }

    }
}
