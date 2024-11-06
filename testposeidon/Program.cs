using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("poseidon test.");
            Random ran = new Random();
            Test(ran);
            Test(ran);
        }
        static void Test(Random ran)
        {
            var count = ran.Next() % 50 + 50;
            UInt64[] data = new UInt64[count];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (ulong)ran.Next();
            }
            var r = poseidon(data);
            Console.WriteLine("r[0]=" + r[0]);
            Console.WriteLine("r[1]=" + r[1]);
            Console.WriteLine("r[2]=" + r[2]);
            Console.WriteLine("r[3]=" + r[3]);
        }
        /// void poseidon(uint64_t* data, uint32_t size, uint64_t* r)
        /// {
        ///     int i;
        ///     poseidon_new(size);
        ///     for(i=0; i<size; i=++) {
        ///         uint64_t* a = data[i];
        ///         poseidon_push(data[i]);
        ///     }
        ///     r[0] = poseidon_finalize();
        ///     r[1] = poseidon_finalize();
        ///     r[2] = poseidon_finalize();
        ///     r[3] = poseidon_finalize();
        ///     wasm_dbg(r[0]);
        ///     wasm_dbg(r[1]);
        ///     wasm_dbg(r[2]);
        ///     wasm_dbg(r[3]);
        /// }
        static UInt64[] poseidon(UInt64[] data)
        {
            UInt64[] result = new UInt64[4];
            host.Poseidon.poseidon_new((ulong)1);

            //32字节对齐
            int value0 = 0;
            for (var i = 0; i < data.Length; i++)
            {
                host.Poseidon.poseidon_push((ulong)data[i]);
                value0++;
                if (value0 == 32)
                {
                    host.Poseidon.poseidon_finalize();
                    host.Poseidon.poseidon_finalize();
                    host.Poseidon.poseidon_finalize();
                    host.Poseidon.poseidon_finalize();
                    host.Poseidon.poseidon_new(0);
                    value0 = 0;
                }
            }

            //32字节对齐 finalize
            host.Poseidon.poseidon_push((ulong)1);
            for (var i2 = value0; i2 < 32; i2++)
            {
                host.Poseidon.poseidon_push((ulong)1);
                value0++;
            }
            result[0] = host.Poseidon.poseidon_finalize();
            result[1] = host.Poseidon.poseidon_finalize();
            result[2] = host.Poseidon.poseidon_finalize();
            result[3] = host.Poseidon.poseidon_finalize();
            return result;
        }
    }
}
