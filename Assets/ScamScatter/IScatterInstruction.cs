using System.Collections.Generic;

namespace ScamScatter
{
    public interface IScatterInstruction
    {
        void PrepareScatter(ScatterCommands commands);
    }
}
