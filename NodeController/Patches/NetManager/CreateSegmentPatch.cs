namespace NodeController.Patches._NetManager
{
    using HarmonyLib;
    using NodeController;
    using NodeController.LifeCycle;
    using KianCommons;
    using static KianCommons.HelpersExtensions;
    using NodeController.Patches._NetTool;


    // TODO check compat with ParallelRoadTool
    [HarmonyPatch(typeof(global::NetManager), nameof(NetManager.CreateSegment))]
    public static class CreateSegmentPatch
    {
        public static void Postfix(ref ushort segment, bool __result)
        {
            if (!__result || !InSimulationThread()) return;

            if (MoveMiddleNodePatch.CopyData) {
                MoveItIntegration.PasteSegment(segment, MoveMiddleNodePatch.SegmentData, null);
            } else if (SplitSegmentPatch.CopyData) {
                MoveItIntegration.PasteSegment(segment, SplitSegmentPatch.SegmentData, null);
            }
        }
    }
}
