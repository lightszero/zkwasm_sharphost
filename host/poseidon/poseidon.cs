using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace host
{

    public static class Poseidon
    {
        static Poseidon()
        {
            if (!Environment.Is64BitProcess)
            {
                throw new Exception("only support x64.");
            }
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var path = AppDomain.CurrentDomain.BaseDirectory;
                var srcpath = System.IO.Path.Combine(path, "poseidon/win_x64/lib_poseidon.dll");
                var destpath = System.IO.Path.Combine(path, "lib_poseidon.dll");
                System.IO.File.Copy(srcpath, destpath, true);
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var path = AppDomain.CurrentDomain.BaseDirectory;
                var srcpath = System.IO.Path.Combine(path, "poseidon/linux_x64/liblib_poseidon.so");
                var destpath = System.IO.Path.Combine(path, "liblib_poseidon.so");
                System.IO.File.Copy(srcpath, destpath, true);
            }
        }
        const string libname = "lib_poseidon";

        //这几个函数实现在由Rust编译出的动态库中
        [DllImport(libname)]
        public static extern void poseidon_new(UInt64 arg);
        [DllImport(libname)]
        public static extern void poseidon_push(UInt64 arg);
        [DllImport(libname)]
        public static extern UInt64 poseidon_finalize();
    }
}
