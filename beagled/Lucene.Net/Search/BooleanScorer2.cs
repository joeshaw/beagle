/*
 * Copyright 2005 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
namespace Lucene.Net.Search
{
	
    /// <summary>An alternative to BooleanScorer.
    /// <br>Uses ConjunctionScorer, DisjunctionScorer, ReqOptScorer and ReqExclScorer.
    /// <br>Implements SkipTo(), and has no limitations on the numbers of added scorers.
    /// </summary>
    public class BooleanScorer2 : Scorer
    {
        private class AnonymousClassDisjunctionSumScorer : DisjunctionSumScorer
        {
            private void  InitBlock(BooleanScorer2 enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private BooleanScorer2 enclosingInstance;
            public BooleanScorer2 Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
				
            }
            internal AnonymousClassDisjunctionSumScorer(BooleanScorer2 enclosingInstance, System.Collections.IList Param1) : base(Param1)
            {
                InitBlock(enclosingInstance);
            }
            public override float Score()
            {
                Enclosing_Instance.coordinator.nrMatchers += nrMatchers;
                return base.Score();
            }
        }

        private class AnonymousClassConjunctionScorer : ConjunctionScorer
        {
            private void  InitBlock(int requiredNrMatchers, BooleanScorer2 enclosingInstance)
            {
                this.requiredNrMatchers = requiredNrMatchers;
                this.enclosingInstance = enclosingInstance;
            }

            private int requiredNrMatchers;
            private BooleanScorer2 enclosingInstance;
            public BooleanScorer2 Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
				
            }
            internal AnonymousClassConjunctionScorer(int requiredNrMatchers, BooleanScorer2 enclosingInstance, Lucene.Net.Search.Similarity Param1) : base(Param1)
            {
                InitBlock(requiredNrMatchers, enclosingInstance);
            }
            public override float Score()
            {
                Enclosing_Instance.coordinator.nrMatchers += requiredNrMatchers;
                // All scorers match, so defaultSimilarity super.score() always has 1 as
                // the coordination factor.
                // Therefore the sum of the scores of the requiredScorers
                // is used as score.
                return base.Score();
            }
        }
        private System.Collections.ArrayList requiredScorers = new System.Collections.ArrayList();
        private System.Collections.ArrayList optionalScorers = new System.Collections.ArrayList();
        private System.Collections.ArrayList prohibitedScorers = new System.Collections.ArrayList();
		
		
        private class Coordinator
        {
            public Coordinator(BooleanScorer2 enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }
            private void  InitBlock(BooleanScorer2 enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private BooleanScorer2 enclosingInstance;
            public BooleanScorer2 Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
				
            }
            internal int maxCoord = 0; // to be increased for each non prohibited scorer
			
            private float[] coordFactors = null;
			
            internal virtual void  Init()
            {
                // use after all scorers have been added.
                coordFactors = new float[maxCoord + 1];
                Similarity sim = Enclosing_Instance.GetSimilarity();
                for (int i = 0; i <= maxCoord; i++)
                {
                    coordFactors[i] = sim.Coord(i, maxCoord);
                }
            }
			
            internal int nrMatchers; // to be increased by score() of match counting scorers.
			
            internal virtual void  InitDoc()
            {
                nrMatchers = 0;
            }
			
            internal virtual float CoordFactor()
            {
                return coordFactors[nrMatchers];
            }
        }
		
        private Coordinator coordinator;
		
        /// <summary>The scorer to which all scoring will be delegated,
        /// except for computing and using the coordination factor.
        /// </summary>
        private Scorer countingSumScorer = null;
		
        public BooleanScorer2(Similarity similarity) : base(similarity)
        {
            coordinator = new Coordinator(this);
        }
		
        public virtual void  Add(Scorer scorer, bool required, bool prohibited)
        {
            if (!prohibited)
            {
                coordinator.maxCoord++;
            }
			
            if (required)
            {
                if (prohibited)
                {
                    throw new System.ArgumentException("scorer cannot be required and prohibited");
                }
                requiredScorers.Add(scorer);
            }
            else if (prohibited)
            {
                prohibitedScorers.Add(scorer);
            }
            else
            {
                optionalScorers.Add(scorer);
            }
        }
		
        /// <summary>Initialize the match counting scorer that sums all the
        /// scores. <p>
        /// When "counting" is used in a name it means counting the number
        /// of matching scorers.<br>
        /// When "sum" is used in a name it means score value summing
        /// over the matching scorers
        /// </summary>
        private void  InitCountingSumScorer()
        {
            coordinator.Init();
            countingSumScorer = MakeCountingSumScorer();
        }
		
        /// <summary>Count a scorer as a single match. </summary>
        private class SingleMatchScorer : Scorer
        {
            private void  InitBlock(BooleanScorer2 enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private BooleanScorer2 enclosingInstance;
            public BooleanScorer2 Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
				
            }
            private Scorer scorer;
            internal SingleMatchScorer(BooleanScorer2 enclosingInstance, Scorer scorer):base(scorer.GetSimilarity())
            {
                InitBlock(enclosingInstance);
                this.scorer = scorer;
            }
            public override float Score()
            {
                Enclosing_Instance.coordinator.nrMatchers++;
                return scorer.Score();
            }
            public override int Doc()
            {
                return scorer.Doc();
            }
            public override bool Next()
            {
                return scorer.Next();
            }
            public override bool SkipTo(int docNr)
            {
                return scorer.SkipTo(docNr);
            }
            public override Explanation Explain(int docNr)
            {
                return scorer.Explain(docNr);
            }
        }
		
        private Scorer CountingDisjunctionSumScorer(System.Collections.IList scorers)
            // each scorer from the list counted as a single matcher
        {
            return new AnonymousClassDisjunctionSumScorer(this, scorers);
        }
		
        private static Similarity defaultSimilarity = new DefaultSimilarity();
		
        private Scorer CountingConjunctionSumScorer(System.Collections.IList requiredScorers)
            // each scorer from the list counted as a single matcher
        {
            int requiredNrMatchers = requiredScorers.Count;
            ConjunctionScorer cs = new AnonymousClassConjunctionScorer(requiredNrMatchers, this, defaultSimilarity);
            System.Collections.IEnumerator rsi = requiredScorers.GetEnumerator();
            while (rsi.MoveNext())
            {
                cs.Add((Scorer) rsi.Current);
            }
            return cs;
        }
		
        /// <summary>Returns the scorer to be used for match counting and score summing.
        /// Uses requiredScorers, optionalScorers and prohibitedScorers.
        /// </summary>
        private Scorer MakeCountingSumScorer()
            // each scorer counted as a single matcher
        {
            if (requiredScorers.Count == 0)
            {
                if (optionalScorers.Count == 0)
                {
                    return new NonMatchingScorer(); // only prohibited scorers
                }
                else if (optionalScorers.Count == 1)
                {
                    return MakeCountingSumScorer2(new SingleMatchScorer(this, (Scorer) optionalScorers[0]), new System.Collections.ArrayList()); // no optional scorers left
                }
                else
                {
                    // more than 1 optionalScorers, no required scorers
                    return MakeCountingSumScorer2(CountingDisjunctionSumScorer(optionalScorers), new System.Collections.ArrayList()); // no optional scorers left
                }
            }
            else if (requiredScorers.Count == 1)
            {
                // 1 required
                return MakeCountingSumScorer2(new SingleMatchScorer(this, (Scorer) requiredScorers[0]), optionalScorers);
            }
            else
            {
                // more required scorers
                return MakeCountingSumScorer2(CountingConjunctionSumScorer(requiredScorers), optionalScorers);
            }
        }
		
        /// <summary>Returns the scorer to be used for match counting and score summing.
        /// Uses the arguments and prohibitedScorers.
        /// </summary>
        /// <param name="requiredCountingSumScorer">A required scorer already built.
        /// </param>
        /// <param name="optionalScorers">A list of optional scorers, possibly empty.
        /// </param>
        private Scorer MakeCountingSumScorer2(Scorer requiredCountingSumScorer, System.Collections.IList optionalScorers)
            // not match counting
        {
            if (optionalScorers.Count == 0)
            {
                // no optional
                if (prohibitedScorers.Count == 0)
                {
                    // no prohibited
                    return requiredCountingSumScorer;
                }
                else if (prohibitedScorers.Count == 1)
                {
                    // no optional, 1 prohibited
                    return new ReqExclScorer(requiredCountingSumScorer, (Scorer) prohibitedScorers[0]); // not match counting
                }
                else
                {
                    // no optional, more prohibited
                    return new ReqExclScorer(requiredCountingSumScorer, new DisjunctionSumScorer(prohibitedScorers)); // score unused. not match counting
                }
            }
            else if (optionalScorers.Count == 1)
            {
                // 1 optional
                return MakeCountingSumScorer3(requiredCountingSumScorer, new SingleMatchScorer(this, (Scorer) optionalScorers[0]));
            }
            else
            {
                // more optional
                return MakeCountingSumScorer3(requiredCountingSumScorer, CountingDisjunctionSumScorer(optionalScorers));
            }
        }
		
        /// <summary>Returns the scorer to be used for match counting and score summing.
        /// Uses the arguments and prohibitedScorers.
        /// </summary>
        /// <param name="requiredCountingSumScorer">A required scorer already built.
        /// </param>
        /// <param name="optionalCountingSumScorer">An optional scorer already built.
        /// </param>
        private Scorer MakeCountingSumScorer3(Scorer requiredCountingSumScorer, Scorer optionalCountingSumScorer)
        {
            if (prohibitedScorers.Count == 0)
            {
                // no prohibited
                return new ReqOptSumScorer(requiredCountingSumScorer, optionalCountingSumScorer);
            }
            else if (prohibitedScorers.Count == 1)
            {
                // 1 prohibited
                return new ReqOptSumScorer(new ReqExclScorer(requiredCountingSumScorer, (Scorer) prohibitedScorers[0]), optionalCountingSumScorer);
            }
            else
            {
                // more prohibited
                return new ReqOptSumScorer(new ReqExclScorer(requiredCountingSumScorer, new DisjunctionSumScorer(prohibitedScorers)), optionalCountingSumScorer);
            }
        }
		
        /// <summary>Scores and collects all matching documents.</summary>
        /// <param name="hc">The collector to which all matching documents are passed through
        /// {@link HitCollector#Collect(int, float)}.
        /// <br>When this method is used the {@link #Explain(int)} method should not be used.
        /// </param>
        public override void  Score(HitCollector hc)
        {
            if (countingSumScorer == null)
            {
                InitCountingSumScorer();
            }
            while (countingSumScorer.Next())
            {
                hc.Collect(countingSumScorer.Doc(), Score());
            }
        }
		
        /// <summary>Expert: Collects matching documents in a range.
        /// <br>Note that {@link #Next()} must be called once before this method is
        /// called for the first time.
        /// </summary>
        /// <param name="hc">The collector to which all matching documents are passed through
        /// {@link HitCollector#Collect(int, float)}.
        /// </param>
        /// <param name="max">Do not score documents past this.
        /// </param>
        /// <returns> true if more matching documents may remain.
        /// </returns>
        protected internal override bool Score(HitCollector hc, int max)
        {
            // null pointer exception when Next() was not called before:
            int docNr = countingSumScorer.Doc();
            while (docNr < max)
            {
                hc.Collect(docNr, Score());
                if (!countingSumScorer.Next())
                {
                    return false;
                }
                docNr = countingSumScorer.Doc();
            }
            return true;
        }
		
        public override int Doc()
        {
            return countingSumScorer.Doc();
        }
		
        public override bool Next()
        {
            if (countingSumScorer == null)
            {
                InitCountingSumScorer();
            }
            return countingSumScorer.Next();
        }
		
        public override float Score()
        {
            coordinator.InitDoc();
            float sum = countingSumScorer.Score();
            return sum * coordinator.CoordFactor();
        }
		
        public override bool SkipTo(int target)
        {
            if (countingSumScorer == null)
            {
                InitCountingSumScorer();
            }
            return countingSumScorer.SkipTo(target);
        }
		
        public override Explanation Explain(int doc)
        {
            throw new System.NotSupportedException();
            /* How to explain the coordination factor?
            initCountingSumScorer();
            return countingSumScorer.explain(doc); // misses coord factor. 
            */
        }
    }
}