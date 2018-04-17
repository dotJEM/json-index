using System;
using System.Collections.Generic;

namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Optimizer
{
    public class TwinConsumerCollection<TIn, TOut1, TOut2>
    {
        //private IEnumerable<TIn> source;
        //private readonly Func<TIn, (TOut1, TOut2)> splitter;

        public IEnumerable<TOut1> Out1 { get; }
        public IEnumerable<TOut2> Out2 { get; }

        public TwinConsumerCollection(IEnumerable<TIn> source, Func<TIn, (TOut1, TOut2)> splitter)
        {
            //this.source = source;
            //this.splitter = splitter;
            //TODO: Candidate for some sort of coordinated Producer/Consumer implementation where the Source is the consumer and it produces for the two outputs using the splitter.

            List<TOut1> out1 = new List<TOut1>();
            List<TOut2> out2 = new List<TOut2>();
            foreach (TIn @in in source)
            {
                (TOut1 o1, TOut2 o2) = splitter(@in);
                out1.Add(o1);
                out2.Add(o2);
            }

            Out1 = out1;
            Out2 = out2;
        }



    }
}