using System;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers
{
	public class QueryParserTokenManager : QueryParserConstants
	{
		public TextWriter debugStream = Console.Out;
		public  void setDebugStream(TextWriter  ds) { debugStream = ds; }
		private int jjStopStringLiteralDfa_3(int pos, long active0)
		{
			switch (pos)
			{
				default :
					return -1;
			}
		}
		private int jjStartNfa_3(int pos, long active0)
		{
			return jjMoveNfa_3(jjStopStringLiteralDfa_3(pos, active0), pos + 1);
		}
		private int jjStopAtPos(int pos, int kind)
		{
			jjmatchedKind = kind;
			jjmatchedPos = pos;
			return pos + 1;
		}
		private int jjStartNfaWithStates_3(int pos, int kind, int state)
		{
			jjmatchedKind = kind;
			jjmatchedPos = pos;
			try { curChar = input_stream.ReadChar(); }
			catch(IOException) { return pos + 1; }
			return jjMoveNfa_3(state, pos + 1);
		}
		private int jjMoveStringLiteralDfa0_3()
		{
			switch ((int)curChar)
			{
				case 40:
					return jjStopAtPos(0, 12);
				case 41:
					return jjStopAtPos(0, 13);
				case 43:
					return jjStopAtPos(0, 10);
				case 45:
					return jjStopAtPos(0, 11);
				case 58:
					return jjStopAtPos(0, 14);
				case 91:
					return jjStopAtPos(0, 22);
				case 94:
					return jjStopAtPos(0, 15);
				case 123:
					return jjStopAtPos(0, 23);
				case 126:
					return jjStartNfaWithStates_3(0, 18, 18);
				default :
					return jjMoveNfa_3(0, 0);
			}
		}
		private void jjCheckNAdd(int state)
		{
			if (jjrounds[state] != jjround)
			{
				jjstateSet[jjnewStateCnt++] = state;
				jjrounds[state] = jjround;
			}
		}
		private void jjAddStates(int start, int end)
		{
			do 
			{
				jjstateSet[jjnewStateCnt++] = jjnextStates[start];
			} while (start++ != end);
		}
		private void jjCheckNAddTwoStates(int state1, int state2)
		{
			jjCheckNAdd(state1);
			jjCheckNAdd(state2);
		}
		private void jjCheckNAddStates(int start, int end)
		{
			do 
			{
				jjCheckNAdd(jjnextStates[start]);
			} while (start++ != end);
		}
		private void jjCheckNAddStates(int start)
		{
			jjCheckNAdd(jjnextStates[start]);
			jjCheckNAdd(jjnextStates[start + 1]);
		}
		static ulong[] jjbitVec0 = {
									   0xfffffffffffffffeUL, 0xffffffffffffffffUL, 0xffffffffffffffffUL, 0xffffffffffffffffUL
								   };
		static ulong[] jjbitVec2 = {
									   0x0UL, 0x0UL, 0xffffffffffffffffUL, 0xffffffffffffffffUL
								   };
		private int jjMoveNfa_3(int startState, int curPos)
		{
			int startsAt = 0;
			jjnewStateCnt = 31;
			int i = 1;
			jjstateSet[0] = startState;
			int kind = 0x7fffffff;
			for (;;)
			{
				if (++jjround == 0x7fffffff)
					ReInitRounds();
				if (curChar < 64)
				{
					ulong l = 1ul << curChar;
					do
					{
						switch(jjstateSet[--i])
						{
							case 0:
								if ((0x7bffd0f8fffffdffL & l) != 0UL)
								{
									if (kind > 17)
										kind = 17;
									jjCheckNAddStates(0, 6);
								}
								else if ((0x100000200L & l) != 0UL)
								{
									if (kind > 6)
										kind = 6;
								}
								else if (curChar == 34)
									jjCheckNAdd(15);
								else if (curChar == 33)
								{
									if (kind > 9)
										kind = 9;
								}
								if (curChar == 38)
									jjstateSet[jjnewStateCnt++] = 4;
								break;
							case 4:
								if (curChar == 38 && kind > 7)
									kind = 7;
								break;
							case 5:
								if (curChar == 38)
									jjstateSet[jjnewStateCnt++] = 4;
								break;
							case 13:
								if (curChar == 33 && kind > 9)
									kind = 9;
								break;
							case 14:
								if (curChar == 34)
									jjCheckNAdd(15);
								break;
							case 15:
								if ((0xfffffffbffffffffUL & l) != 0UL)
									jjCheckNAddTwoStates(15, 16);
								break;
							case 16:
								if (curChar == 34 && kind > 16)
									kind = 16;
								break;
							case 18:
								if ((0x3ff000000000000UL & l) == 0UL)
									break;
								if (kind > 19)
									kind = 19;
								jjstateSet[jjnewStateCnt++] = 18;
								break;
							case 19:
								if ((0x7bffd0f8fffffdffUL & l) == 0UL)
									break;
								if (kind > 17)
									kind = 17;
								jjCheckNAddStates(0, 6);
								break;
							case 20:
								if ((0x7bffd0f8fffffdffUL & l) == 0UL)
									break;
								if (kind > 17)
									kind = 17;
								jjCheckNAddTwoStates(20, 21);
								break;
							case 22:
								if ((0x84002f0600000000UL & l) == 0UL)
									break;
								if (kind > 17)
									kind = 17;
								jjCheckNAddTwoStates(20, 21);
								break;
							case 23:
								if ((0x7bffd0f8fffffdffUL & l) != 0UL)
									jjCheckNAddStates(7, 9);
								break;
							case 24:
								if (curChar == 42 && kind > 20)
									kind = 20;
								break;
							case 26:
								if ((0x84002f0600000000UL & l) != 0UL)
									jjCheckNAddStates(7, 9);
								break;
							case 27:
								if ((0xfbffd4f8fffffdffUL & l) == 0UL)
									break;
								if (kind > 21)
									kind = 21;
								jjCheckNAddTwoStates(27, 28);
								break;
							case 29:
								if ((0x84002f0600000000UL & l) == 0UL)
									break;
								if (kind > 21)
									kind = 21;
								jjCheckNAddTwoStates(27, 28);
								break;
							default : break;
						}
					} while(i != startsAt);
				}
				else if (curChar < 128)
				{
					ulong l = 1ul << (curChar & 63);
					do
					{
						switch(jjstateSet[--i])
						{
							case 0:
								if ((0x97ffffff97ffffffUL & l) != 0UL)
								{
									if (kind > 17)
										kind = 17;
									jjCheckNAddStates(0, 6);
								}
								else if (curChar == 126)
									jjstateSet[jjnewStateCnt++] = 18;
								if (curChar == 92)
									jjCheckNAddStates(10, 12);
								else if (curChar == 78)
									jjstateSet[jjnewStateCnt++] = 11;
								else if (curChar == 124)
									jjstateSet[jjnewStateCnt++] = 8;
								else if (curChar == 79)
									jjstateSet[jjnewStateCnt++] = 6;
								else if (curChar == 65)
									jjstateSet[jjnewStateCnt++] = 2;
								break;
							case 1:
								if (curChar == 68 && kind > 7)
									kind = 7;
								break;
							case 2:
								if (curChar == 78)
									jjstateSet[jjnewStateCnt++] = 1;
								break;
							case 3:
								if (curChar == 65)
									jjstateSet[jjnewStateCnt++] = 2;
								break;
							case 6:
								if (curChar == 82 && kind > 8)
									kind = 8;
								break;
							case 7:
								if (curChar == 79)
									jjstateSet[jjnewStateCnt++] = 6;
								break;
							case 8:
								if (curChar == 124 && kind > 8)
									kind = 8;
								break;
							case 9:
								if (curChar == 124)
									jjstateSet[jjnewStateCnt++] = 8;
								break;
							case 10:
								if (curChar == 84 && kind > 9)
									kind = 9;
								break;
							case 11:
								if (curChar == 79)
									jjstateSet[jjnewStateCnt++] = 10;
								break;
							case 12:
								if (curChar == 78)
									jjstateSet[jjnewStateCnt++] = 11;
								break;
							case 15:
								jjAddStates(13, 14);
								break;
							case 17:
								if (curChar == 126)
									jjstateSet[jjnewStateCnt++] = 18;
								break;
							case 19:
								if ((0x97ffffff97ffffffUL & l) == 0UL)
									break;
								if (kind > 17)
									kind = 17;
								jjCheckNAddStates(0, 6);
								break;
							case 20:
								if ((0x97ffffff97ffffffUL & l) == 0UL)
									break;
								if (kind > 17)
									kind = 17;
								jjCheckNAddTwoStates(20, 21);
								break;
							case 21:
								if (curChar == 92)
									jjCheckNAddTwoStates(22, 22);
								break;
							case 22:
								if ((0x6800000078000000UL & l) == 0UL)
									break;
								if (kind > 17)
									kind = 17;
								jjCheckNAddTwoStates(20, 21);
								break;
							case 23:
								if ((0x97ffffff97ffffffUL & l) != 0UL)
									jjCheckNAddStates(7, 9);
								break;
							case 25:
								if (curChar == 92)
									jjCheckNAddTwoStates(26, 26);
								break;
							case 26:
								if ((0x6800000078000000UL & l) != 0UL)
									jjCheckNAddStates(7, 9);
								break;
							case 27:
								if ((0x97ffffff97ffffffUL & l) == 0UL)
									break;
								if (kind > 21)
									kind = 21;
								jjCheckNAddTwoStates(27, 28);
								break;
							case 28:
								if (curChar == 92)
									jjCheckNAddTwoStates(29, 29);
								break;
							case 29:
								if ((0x6800000078000000UL & l) == 0UL)
									break;
								if (kind > 21)
									kind = 21;
								jjCheckNAddTwoStates(27, 28);
								break;
							case 30:
								if (curChar == 92)
									jjCheckNAddStates(10, 12);
								break;
							default : break;
						}
					} while(i != startsAt);
				}
				else
				{
					int hiByte = (int)(curChar >> 8);
					int i1 = hiByte >> 6;
					long l1 = 1L << (hiByte & 63);
					int i2 = (curChar & 0xff) >> 6;
					long l2 = 1L << (curChar & 63);
					do
					{
						switch(jjstateSet[--i])
						{
							case 0:
								if (!jjCanMove_0(hiByte, i1, i2, l1, l2))
									break;
								if (kind > 17)
									kind = 17;
								jjCheckNAddStates(0, 6);
								break;
							case 15:
								if (jjCanMove_0(hiByte, i1, i2, l1, l2))
									jjAddStates(13, 14);
								break;
							case 20:
								if (!jjCanMove_0(hiByte, i1, i2, l1, l2))
									break;
								if (kind > 17)
									kind = 17;
								jjCheckNAddTwoStates(20, 21);
								break;
							case 23:
								if (jjCanMove_0(hiByte, i1, i2, l1, l2))
									jjCheckNAddStates(7, 9);
								break;
							case 27:
								if (!jjCanMove_0(hiByte, i1, i2, l1, l2))
									break;
								if (kind > 21)
									kind = 21;
								jjCheckNAddTwoStates(27, 28);
								break;
							default : break;
						}
					} while(i != startsAt);
				}
				if (kind != 0x7fffffff)
				{
					jjmatchedKind = kind;
					jjmatchedPos = curPos;
					kind = 0x7fffffff;
				}
				++curPos;
				if ((i = jjnewStateCnt) == (startsAt = 31 - (jjnewStateCnt = startsAt)))
					return curPos;
				try { curChar = input_stream.ReadChar(); }
				catch(IOException) { return curPos; }
			}
		}
		private int jjStopStringLiteralDfa_1(int pos, long active0)
		{
			switch (pos)
			{
				case 0:
					if ((active0 & 0x20000000L) != 0L)
					{
						jjmatchedKind = 32;
						return 4;
					}
					return -1;
				default :
					return -1;
			}
		}
		private int jjStartNfa_1(int pos, long active0)
		{
			return jjMoveNfa_1(jjStopStringLiteralDfa_1(pos, active0), pos + 1);
		}
		private int jjStartNfaWithStates_1(int pos, int kind, int state)
		{
			jjmatchedKind = kind;
			jjmatchedPos = pos;
			try { curChar = input_stream.ReadChar(); }
			catch(IOException) { return pos + 1; }
			return jjMoveNfa_1(state, pos + 1);
		}
		private int jjMoveStringLiteralDfa0_1()
		{
			switch(curChar)
			{
				case (char)84:
					return jjMoveStringLiteralDfa1_1(0x20000000L);
				case (char)125:
					return jjStopAtPos(0, 30);
				default :
					return jjMoveNfa_1(0, 0);
			}
		}
		private int jjMoveStringLiteralDfa1_1(long active0)
		{
			try { curChar = input_stream.ReadChar(); }
			catch(IOException) 
			{
				jjStopStringLiteralDfa_1(0, active0);
				return 1;
			}
			switch(curChar)
			{
				case (char)79:
					if ((active0 & 0x20000000L) != 0L)
						return jjStartNfaWithStates_1(1, 29, 4);
					break;
				default :
					break;
			}
			return jjStartNfa_1(0, active0);
		}
		private int jjMoveNfa_1(int startState, int curPos)
		{
			int startsAt = 0;
			jjnewStateCnt = 5;
			int i = 1;
			jjstateSet[0] = startState;
			int  kind = 0x7fffffff;
			for (;;)
			{
				if (++jjround == 0x7fffffff)
					ReInitRounds();
				if (curChar < 64)
				{
					ulong l = 1ul << curChar;
					do
					{
						switch(jjstateSet[--i])
						{
							case 0:
								if ((0xfffffffeffffffffUL & l) != 0UL)
								{
									if (kind > 32)
										kind = 32;
									jjCheckNAdd(4);
								}
								if ((0x100000200UL & l) != 0UL)
								{
									if (kind > 6)
										kind = 6;
								}
								else if (curChar == 34)
									jjCheckNAdd(2);
								break;
							case 1:
								if (curChar == 34)
									jjCheckNAdd(2);
								break;
							case 2:
								if ((0xfffffffbffffffffUL & l) != 0UL)
									jjCheckNAddTwoStates(2, 3);
								break;
							case 3:
								if (curChar == 34 && kind > 31)
									kind = 31;
								break;
							case 4:
								if ((0xfffffffeffffffffUL & l) == 0UL)
									break;
								if (kind > 32)
									kind = 32;
								jjCheckNAdd(4);
								break;
							default : break;
						}
					} while(i != startsAt);
				}
				else if (curChar < 128)
				{
					ulong l = 1ul << (curChar & 63);
					do
					{
						switch(jjstateSet[--i])
						{
							case 0:
							case 4:
								if ((0xdfffffffffffffffUL & l) == 0UL)
									break;
								if (kind > 32)
									kind = 32;
								jjCheckNAdd(4);
								break;
							case 2:
								jjAddStates(15, 16);
								break;
							default : break;
						}
					} while(i != startsAt);
				}
				else
				{
					int hiByte = (int)(curChar >> 8);
					int i1 = hiByte >> 6;
					long l1 = 1L << (hiByte & 63);
					int i2 = (curChar & 0xff) >> 6;
					long l2 = 1L << (curChar & 63);
					do
					{
						switch(jjstateSet[--i])
						{
							case 0:
							case 4:
								if (!jjCanMove_0(hiByte, i1, i2, l1, l2))
									break;
								if (kind > 32)
									kind = 32;
								jjCheckNAdd(4);
								break;
							case 2:
								if (jjCanMove_0(hiByte, i1, i2, l1, l2))
									jjAddStates(15, 16);
								break;
							default : break;
						}
					} while(i != startsAt);
				}
				if (kind != 0x7fffffff)
				{
					jjmatchedKind = kind;
					jjmatchedPos = curPos;
					kind = 0x7fffffff;
				}
				++curPos;
				if ((i = jjnewStateCnt) == (startsAt = 5 - (jjnewStateCnt = startsAt)))
					return curPos;
				try { curChar = input_stream.ReadChar(); }
				catch(IOException) { return curPos; }
			}
		}
		private int jjMoveStringLiteralDfa0_0()
		{
			return jjMoveNfa_0(0, 0);
		}
		private int jjMoveNfa_0(int startState, int curPos)
		{
			int startsAt = 0;
			jjnewStateCnt = 3;
			int i = 1;
			jjstateSet[0] = startState;
			int kind = 0x7fffffff;
			for (;;)
			{
				if (++jjround == 0x7fffffff)
					ReInitRounds();
				if (curChar < 64)
				{
					ulong l = 1ul << curChar;
					do
					{
						switch(jjstateSet[--i])
						{
							case 0:
								if ((0x3ff000000000000UL & l) == 0UL)
									break;
								if (kind > 24)
									kind = 24;
								jjAddStates(17, 18);
								break;
							case 1:
								if (curChar == 46)
									jjCheckNAdd(2);
								break;
							case 2:
								if ((0x3ff000000000000UL & l) == 0UL)
									break;
								if (kind > 24)
									kind = 24;
								jjCheckNAdd(2);
								break;
							default : break;
						}
					} while(i != startsAt);
				}
				else if (curChar < 128)
				{
					ulong l = 1ul << (curChar & 63);
					do
					{
						switch(jjstateSet[--i])
						{
							default : break;
						}
					} while(i != startsAt);
				}
				else
				{
					int hiByte = (int)(curChar >> 8);
					int i1 = hiByte >> 6;
					long l1 = 1L << (hiByte & 63);
					int i2 = (curChar & 0xff) >> 6;
					long l2 = 1L << (curChar & 63);
					do
					{
						switch(jjstateSet[--i])
						{
							default : break;
						}
					} while(i != startsAt);
				}
				if (kind != 0x7fffffff)
				{
					jjmatchedKind = kind;
					jjmatchedPos = curPos;
					kind = 0x7fffffff;
				}
				++curPos;
				if ((i = jjnewStateCnt) == (startsAt = 3 - (jjnewStateCnt = startsAt)))
					return curPos;
				try { curChar = input_stream.ReadChar(); }
				catch(IOException) { return curPos; }
			}
		}
		private int jjStopStringLiteralDfa_2(int pos, long active0)
		{
			switch (pos)
			{
				case 0:
					if ((active0 & 0x2000000L) != 0L)
					{
						jjmatchedKind = 28;
						return 4;
					}
					return -1;
				default :
					return -1;
			}
		}
		private int jjStartNfa_2(int pos, long active0)
		{
			return jjMoveNfa_2(jjStopStringLiteralDfa_2(pos, active0), pos + 1);
		}
		private int jjStartNfaWithStates_2(int pos, int kind, int state)
		{
			jjmatchedKind = kind;
			jjmatchedPos = pos;
			try { curChar = input_stream.ReadChar(); }
			catch(IOException) { return pos + 1; }
			return jjMoveNfa_2(state, pos + 1);
		}
		private int jjMoveStringLiteralDfa0_2()
		{
			switch(curChar)
			{
				case (char)84:
					return jjMoveStringLiteralDfa1_2(0x2000000L);
				case (char)93:
					return jjStopAtPos(0, 26);
				default :
					return jjMoveNfa_2(0, 0);
			}
		}
		private int jjMoveStringLiteralDfa1_2(long active0)
		{
			try { curChar = input_stream.ReadChar(); }
			catch(IOException) 
			{
				jjStopStringLiteralDfa_2(0, active0);
				return 1;
			}
			switch(curChar)
			{
				case (char)79:
					if ((active0 & 0x2000000L) != 0L)
						return jjStartNfaWithStates_2(1, 25, 4);
					break;
				default :
					break;
			}
			return jjStartNfa_2(0, active0);
		}
		private int jjMoveNfa_2(int startState, int curPos)
		{
			int startsAt = 0;
			jjnewStateCnt = 5;
			int i = 1;
			jjstateSet[0] = startState;
			int kind = 0x7fffffff;
			for (;;)
			{
				if (++jjround == 0x7fffffff)
					ReInitRounds();
				if (curChar < 64)
				{
					ulong l = 1ul << curChar;
					do
					{
						switch(jjstateSet[--i])
						{
							case 0:
								if ((0xfffffffeffffffffUL & l) != 0UL)
								{
									if (kind > 28)
										kind = 28;
									jjCheckNAdd(4);
								}
								if ((0x100000200UL & l) != 0UL)
								{
									if (kind > 6)
										kind = 6;
								}
								else if (curChar == 34)
									jjCheckNAdd(2);
								break;
							case 1:
								if (curChar == 34)
									jjCheckNAdd(2);
								break;
							case 2:
								if ((0xfffffffbffffffffUL & l) != 0UL)
									jjCheckNAddTwoStates(2, 3);
								break;
							case 3:
								if (curChar == 34 && kind > 27)
									kind = 27;
								break;
							case 4:
								if ((0xfffffffeffffffffUL & l) == 0UL)
									break;
								if (kind > 28)
									kind = 28;
								jjCheckNAdd(4);
								break;
							default : break;
						}
					} while(i != startsAt);
				}
				else if (curChar < 128)
				{
					ulong l = 1ul << (curChar & 63);
					do
					{
						switch(jjstateSet[--i])
						{
							case 0:
							case 4:
								if ((0xffffffffdfffffffUL & l) == 0UL)
									break;
								if (kind > 28)
									kind = 28;
								jjCheckNAdd(4);
								break;
							case 2:
								jjAddStates(15, 16);
								break;
							default : break;
						}
					} while(i != startsAt);
				}
				else
				{
					int hiByte = (int)(curChar >> 8);
					int i1 = hiByte >> 6;
					long l1 = 1L << (hiByte & 63);
					int i2 = (curChar & 0xff) >> 6;
					long l2 = 1L << (curChar & 63);
					do
					{
						switch(jjstateSet[--i])
						{
							case 0:
							case 4:
								if (!jjCanMove_0(hiByte, i1, i2, l1, l2))
									break;
								if (kind > 28)
									kind = 28;
								jjCheckNAdd(4);
								break;
							case 2:
								if (jjCanMove_0(hiByte, i1, i2, l1, l2))
									jjAddStates(15, 16);
								break;
							default : break;
						}
					} while(i != startsAt);
				}
				if (kind != 0x7fffffff)
				{
					jjmatchedKind = kind;
					jjmatchedPos = curPos;
					kind = 0x7fffffff;
				}
				++curPos;
				if ((i = jjnewStateCnt) == (startsAt = 5 - (jjnewStateCnt = startsAt)))
					return curPos;
				try { curChar = input_stream.ReadChar(); }
				catch(IOException) { return curPos; }
			}
		}
		readonly int[] jjnextStates = new int[] 
		{
			20, 23, 24, 27, 28, 25, 21, 23, 24, 25, 22, 26, 29, 15, 16, 2, 
			3, 0, 1, 
		};
		private static bool jjCanMove_0(int hiByte, int i1, int i2, long l1, long l2)
		{
			switch(hiByte)
			{
				case 0:
					return ((jjbitVec2[i2] & (ulong)l2) != 0UL);
				default : 
					if ((jjbitVec0[i1] & (ulong)l1) != 0UL)
						return true;
					return false;
			}
		}
		public readonly String[] jjstrLiteralImages = new String[]
		{
			"", null, null, null, null, null, null, null, null, null, "\u0053", "\u0055", "\u0050", 
			"\u0051", "\u0072", "\u0136", null, null, "\u0176", null, null, null, "\u0133", "\u0173", null, 
			"\u0124\u0117", "\u0135", null, null, "\u0124\u0117", "\u0175", null, null, };
		public static readonly String[] lexStateNames = 
		{
			"Boost", 
			"RangeEx", 
			"RangeIn", 
			"DEFAULT", 
		};
		public readonly int[] jjnewLexState = new int[] 
		{
			-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, -1, -1, -1, -1, -1, -1, 2, 1, 3, 
			-1, 3, -1, -1, -1, 3, -1, -1, 
		};
		static readonly long[] jjtoToken = new long[] 
		{
			0x1ffffff81L, 
		};
		static readonly long[] jjtoSkip = new long[] 
		{
			0x40L, 
		};
		protected CharStream input_stream;
		private readonly uint[] jjrounds = new uint[31];
		private readonly int[] jjstateSet = new int[62];
		protected char curChar;
		public QueryParserTokenManager(CharStream stream)
		{
			input_stream = stream;
		}
		public QueryParserTokenManager(CharStream stream, int lexState)
			: this(stream)
		{
			SwitchTo(lexState);
		}
		public void ReInit(CharStream stream)
		{
			jjmatchedPos = jjnewStateCnt = 0;
			curLexState = defaultLexState;
			input_stream = stream;
			ReInitRounds();
		}
		private void ReInitRounds()
		{
			int i;
			jjround = 0x80000001;
			for (i = 31; i-- > 0;)
				jjrounds[i] = 0x80000000;
		}
		public void ReInit(CharStream stream, int lexState)
		{
			ReInit(stream);
			SwitchTo(lexState);
		}
		public void SwitchTo(int lexState)
		{
			if (lexState >= 4 || lexState < 0)
				throw new TokenMgrError("Error: Ignoring invalid lexical state : " + lexState + ". State unchanged.", TokenMgrError.INVALID_LEXICAL_STATE);
			else
				curLexState = lexState;
		}

		protected Token jjFillToken()
		{
			Token t = Token.NewToken(jjmatchedKind);
			t.kind = jjmatchedKind;
			String im = jjstrLiteralImages[jjmatchedKind];
			t.image = (im == null) ? input_stream.GetImage() : im;
			t.beginLine = input_stream.GetBeginLine();
			t.beginColumn = input_stream.GetBeginColumn();
			t.endLine = input_stream.GetEndLine();
			t.endColumn = input_stream.GetEndColumn();
			return t;
		}

		int curLexState = 3;
		int defaultLexState = 3;
		int jjnewStateCnt;
		uint jjround;
		int jjmatchedPos;
		int jjmatchedKind;

		public Token getNextToken() 
		{
			Token matchedToken;
			int curPos = 0;

			EOFLoop :
				for (;;)
				{   
					try   
					{     
						curChar = input_stream.BeginToken();
					}     
					catch(IOException)
					{        
						jjmatchedKind = 0;
						matchedToken = jjFillToken();
						return matchedToken;
					}

					switch(curLexState)
					{
						case 0:
							jjmatchedKind = 0x7fffffff;
							jjmatchedPos = 0;
							curPos = jjMoveStringLiteralDfa0_0();
							break;
						case 1:
							jjmatchedKind = 0x7fffffff;
							jjmatchedPos = 0;
							curPos = jjMoveStringLiteralDfa0_1();
							break;
						case 2:
							jjmatchedKind = 0x7fffffff;
							jjmatchedPos = 0;
							curPos = jjMoveStringLiteralDfa0_2();
							break;
						case 3:
							jjmatchedKind = 0x7fffffff;
							jjmatchedPos = 0;
							curPos = jjMoveStringLiteralDfa0_3();
							break;
					}
					if (jjmatchedKind != 0x7fffffff)
					{
						if (jjmatchedPos + 1 < curPos)
							input_stream.Backup(curPos - jjmatchedPos - 1);
						if ((jjtoToken[jjmatchedKind >> 6] & (1L << (jjmatchedKind & 63))) != 0L)
						{
							matchedToken = jjFillToken();
							if (jjnewLexState[jjmatchedKind] != -1)
								curLexState = jjnewLexState[jjmatchedKind];
							return matchedToken;
						}
						else
						{
							if (jjnewLexState[jjmatchedKind] != -1)
								curLexState = jjnewLexState[jjmatchedKind];
							goto EOFLoop;
						}
					}
					int error_line = input_stream.GetEndLine();
					int error_column = input_stream.GetEndColumn();
					String error_after = null;
					bool EOFSeen = false;
					try { input_stream.ReadChar(); input_stream.Backup(1); }
					catch (IOException) 
					{
						EOFSeen = true;
						error_after = curPos <= 1 ? "" : input_stream.GetImage();
						if (curChar == '\n' || curChar == '\r') 
						{
							error_line++;
							error_column = 0;
						}
						else
							error_column++;
					}
					if (!EOFSeen) 
					{
						input_stream.Backup(1);
						error_after = curPos <= 1 ? "" : input_stream.GetImage();
					}
					throw new TokenMgrError(EOFSeen, curLexState, error_line, error_column, error_after, curChar, TokenMgrError.LEXICAL_ERROR);
				}
		}

	}
}