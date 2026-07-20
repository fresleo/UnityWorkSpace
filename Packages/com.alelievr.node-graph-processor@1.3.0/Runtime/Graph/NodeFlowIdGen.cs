namespace GraphProcessor
{
    /// <summary>
    /// Id分配器
    /// </summary>
    public class NodeFlowIdGen
    {
        private int _idIndex = 0;

        public NodeFlowIdGen(int startId = 0)
        {
            _idIndex = startId;
        }

        public int GenId()
        {
            return ++_idIndex;
        }
    }
}