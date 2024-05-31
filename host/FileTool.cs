using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace host
{
    internal class FileTool
    {
        public enum FileState
        {
            NotExist,
            InSetup,
            SetupDone,
            SetupFail,
        }
        public static FileState LockState(string filename)
        {

            return FileState.NotExist;
        }
        public static void GetState()
        {

        }
        public static void UnLockState()
        {

        }
    }
}
