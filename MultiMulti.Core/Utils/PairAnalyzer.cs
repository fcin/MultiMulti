using MultiMulti.Core.Services;
using System.Linq;

namespace MultiMulti.Core.Utils
{
    public class PairAnalyzer
    {
        public PairAnalysisResult Analyze(int[] pair, Data[] data)
        {
            var occurences = 0;

            foreach (var draw in data)
            {
                foreach (var currentPair in draw.Values)
                {
                    var values = currentPair.ToArray();
                    if (values[0] == pair[0] && values[1] == pair[1])
                    {
                        occurences++;
                        break;
                    }
                }
            }

            return new PairAnalysisResult
            {
                ValueText = string.Join(", ", pair),
                Pair = pair,
                OccurenceCount = occurences,
                OccurencePercentage = (double)occurences / data.Sum(d => d.Values.Count()) * 100
            };
        }
    }
}
