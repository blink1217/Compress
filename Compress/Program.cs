using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compress
{
    class Program
    {
        static void Main(string[] args)
        { 
            var watch = System.Diagnostics.Stopwatch.StartNew();


            var provider = new ServiceCollection()
                       .AddMemoryCache()
                       .BuildServiceProvider();

            //Should be redis or some kind of persistent cache
            var cache = provider.GetService<IMemoryCache>();

            var fs = new FileStream("dotnet-sdk-3.1.100-win-x64.exe", FileMode.Open);
            var len = (int)fs.Length;
            var bits = new byte[len];
            fs.Read(bits, 0, len);

            Dictionary<int, string> par = new Dictionary<int, string>();
            int index = 0;

            // Dump 16 bytes per line
            for (int ix = 0; ix < len; ix += 16)
            {
                var cnt = Math.Min(16, len - ix);
                var line = new byte[cnt];

                Array.Copy(bits, ix, line, 0, cnt);

                var l = BitConverter.ToString(line);
                par.Add(index, l);
                index++;

            }

            int indexP = 0;

            Parallel.For(0, par.Count(), (i, loopState) =>
            {
                var already = cache.Get(par[i]);
                if(already !=  null)
                {
                    lock (par)
                    {
                        par[i] = already.ToString();
                    }
                }
                else
                {
                    var s = "#" + indexP++;

                    cache.Set(par[i], s);
                    cache.Set(s, par[i]);

                    lock (par)
                    {
                        par[i] = s;
                    }
                }   
            });

            StringBuilder sb = new StringBuilder();
            foreach(var d in par)
            {
                sb.Append(d.Value);
            }

            //file size went from 122921KB to 60336KB in less than a minute ;)
            WriteConvertedFile(sb, "Output.fuk");

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            Console.WriteLine($"Complete in elapsedMs {elapsedMs}");
        }

        public static void WriteConvertedFile(StringBuilder s, string File)
        {
            using StreamWriter file = new StreamWriter(File);
            file.WriteLine(s.ToString());
        }
    }
}
