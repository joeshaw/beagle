using System;
using System.Text;
using System.Threading;
using System.Windows.Forms.RTF;
using System.IO;

namespace TextTestClass {
	public class Test {
		static Test	test;
		int		skip_width;
		int		skip_count;
		TextMap text;

		public Test(string[] args) {
			if (args.Length == 0)
				throw new Exception ("Program needs path to rtf file as argument");

			FileStream	stream;
			RTF		rtf;

			text = new TextMap();
			TextMap.SetupStandardTable(text.Table);

			stream = new FileStream (@"../test.rtf", FileMode.Open);
			rtf = new RTF(stream);

			skip_width = 0;
			skip_count = 0;

			rtf.ClassCallback[TokenClass.Text] = new ClassDelegate(HandleText);
			rtf.ClassCallback[TokenClass.Control] = new ClassDelegate(HandleControl);

			rtf.Read();

			stream.Close();
		}

		void HandleControl(RTF rtf) {
			switch(rtf.Major) {
				case Major.Unicode: {
					switch(rtf.Minor) {
						case Minor.UnicodeCharBytes: {
							skip_width = rtf.Param;
							break;
						}

						case Minor.UnicodeChar: {
							Console.Write("[Unicode {0:X4}]", rtf.Param);
							skip_count += skip_width;
							break;
						}
					}
					break;
				}

				case Major.Destination: {
					Console.Write("[Got Destination control {0}]", rtf.Minor);
					rtf.SkipGroup();
					break;
				}

				case Major.CharAttr: {
					switch(rtf.Minor) {
						case Minor.ForeColor: {
							System.Windows.Forms.RTF.Color	color;
							int	num;

							color = System.Windows.Forms.RTF.Color.GetColor(rtf, rtf.Param);
							if (color != null) {
								if (color.Red == -1 && color.Green == -1 && color.Blue == -1) {
									Console.Write("[Default Color]");
								} else {
									Console.Write("[Color {0} [{1:X2}{2:X2}{3:X}]]", rtf.Param, color.Red, color.Green, color.Blue);
								}
							}
							break;
						}

						case Minor.FontSize: {
							Console.Write("[Fontsize {0}]", rtf.Param);
							break;
						}

						case Minor.FontNum: {
							System.Windows.Forms.RTF.Font	font;

							font = System.Windows.Forms.RTF.Font.GetFont(rtf, rtf.Param);
							if (font != null) {
								Console.Write("[Font {0} [{1}]]", rtf.Param, font.Name);
							}
							break;
						}

						case Minor.Plain: {
							Console.Write("[Normal]");
							break;
						}

						case Minor.Bold: {
							if (rtf.Param == RTF.NoParam) {
								Console.Write("[Bold]");
							} else {
								Console.Write("[NoBold]");
							}
							break;
						}

						case Minor.Italic: {
							if (rtf.Param == RTF.NoParam) {
								Console.Write("[Italic]");
							} else {
								Console.Write("[NoItalic]");
							}
							break;
						}

						case Minor.StrikeThru: {
							if (rtf.Param == RTF.NoParam) {
								Console.Write("[StrikeThru]");
							} else {
								Console.Write("[NoStrikeThru]");
							}
							break;
						}

						case Minor.Underline: {
							if (rtf.Param == RTF.NoParam) {
								Console.Write("[Underline]");
							} else {
								Console.Write("[NoUnderline]");
							}
							break;
						}

						case Minor.NoUnderline: {
							Console.Write("[NoUnderline]");
							break;
						}
					}
					break;
				}

				case Major.SpecialChar: {
					Console.Write("[Got SpecialChar control {0}]", rtf.Minor);
					SpecialChar(rtf);
					break;
				}
			}
		}

		void SpecialChar(RTF rtf) {
			switch(rtf.Minor) {
				case Minor.Page:
				case Minor.Sect:
				case Minor.Row:
				case Minor.Line:
				case Minor.Par: {
					Console.Write("\n");
					break;
				}

				case Minor.Cell: {
					Console.Write(" ");
					break;
				}

				case Minor.NoBrkSpace: {
					Console.Write(" ");
					break;
				}

				case Minor.Tab: {
					Console.Write("\t");
					break;
				}

				case Minor.NoBrkHyphen: {
					Console.Write("-");
					break;
				}

				case Minor.Bullet: {
					Console.Write("*");
					break;
				}

				case Minor.EmDash: {
					Console.Write("");
					break;
				}

				case Minor.EnDash: {
					Console.Write("");
					break;
				}

				case Minor.LQuote: {
					Console.Write("");
					break;
				}

				case Minor.RQuote: {
					Console.Write("");
					break;
				}

				case Minor.LDblQuote: {
					Console.Write("");
					break;
				}

				case Minor.RDblQuote: {
					Console.Write("");
					break;
				}

				default: {
					rtf.SkipGroup();
					break;
				}
			}
		}


		void HandleText(RTF rtf) {
			if (skip_count > 0) {
				skip_count--;
				return;
			}
			if ((StandardCharCode)rtf.Minor != StandardCharCode.nothing) {
				Console.Write("{0}", text[(StandardCharCode)rtf.Minor]);
			} else {
				if ((int)rtf.Major > 31 && (int)rtf.Major < 128) {
					Console.Write("{0}", (char)rtf.Major);
				} else {
					Console.Write("[Literal:0x{0:X2}]", (int)rtf.Major);
				}
			}
		}

		public static void Main(string[] args) {
			test = new Test(args);
		}
	}
}
