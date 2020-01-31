﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Compress
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var watch = System.Diagnostics.Stopwatch.StartNew();


            var provider = new ServiceCollection()
                       .AddMemoryCache()
                       .BuildServiceProvider();

            //Should be redis or some kind of persistant cache
            var cache = provider.GetService<IMemoryCache>();

            var fs = new FileStream("Downloads.zip", FileMode.Open);
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

            var listOne = par
                .GroupBy(x => x.Value)
                .Distinct().AsParallel().ToList();

            int indexP = 0;

            //Could use some kind of custom hardware it's slow on my laptop
            Parallel.For(0, par.Count(), (i, loopState) =>
            {
                foreach(var li in listOne)
                {
                    if (li.FirstOrDefault().Value == par[i])
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
                    }
                }

            });

            StringBuilder sb = new StringBuilder();
            foreach(var d in par)
            {
                sb.Append(d.Value);
            }

            //file size went from 2551KB to 1000KB not sure if it was worth 5 minutes of my day ;)
            WriteConvertedFile(sb, "Output.fuk");

            // the code that you want to measure comes here
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            Console.WriteLine("Complete");
        }

        public static void WriteConvertedFile(StringBuilder s, string File)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(File))
            {
                file.WriteLine(s.ToString());
            }
        }

    }
}
