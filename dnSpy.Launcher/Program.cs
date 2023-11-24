using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace dnSpy.Launcher {
	static class Program {
#if NETFRAMEWORK
		static int Main(string[] args) {
			var mainPath = AppDomain.CurrentDomain.BaseDirectory;
			var x86Path = Path.Combine(mainPath, "dnSpy-x86.exe");
			var x64Path = Path.Combine(mainPath, "dnSpy.exe");

			if (args.Length == 0) {
				try {
					Process.Start(new ProcessStartInfo {
						FileName = Environment.Is64BitOperatingSystem ? x64Path : x86Path
					});
				}
				catch {
					// ignored
				}
			}
			else {
				try {
					var sb = new StringBuilder();
					var arg = args[0];
					var arch = GetArchValue(arg);
					if (arch.HasValue) {
						sb.Append($@"""{arg}"" ");
					}

					if (args.Length > 1) {
						for (int i = 1; i < args.Length; i++)
							sb.Append($@"""{args[i]}"" ");
					}

					if (arch.HasValue) {
						Process.Start(new ProcessStartInfo {
							Arguments = sb.ToString(), FileName = arch == 64 ? x64Path : x86Path
						});
					}
				}
				catch {
					// ignored
				}
			}

			return 0;
		}

		// https://github.com/dnSpyEx/dnSpy/blob/master/Extensions/dnSpy.Debugger/dnSpy.Debugger/DbgUI/StartDebuggingOptionsProvider.cs#L136
		static int? GetArchValue(string fileName) {
			int? arch = null;
			try {
				using var peImage = new PEImage(fileName, false);
				var machine = peImage.ImageNTHeaders.FileHeader.Machine;
				if (machine.Is64Bit())
					arch = 64;
				else if (machine.IsI386()) {
					var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
					var isDotNet = dotNetDir.VirtualAddress != 0;
					if (isDotNet) {
						var cor20HeaderReader = peImage.CreateReader(dotNetDir.VirtualAddress, 0x48);
						var cor20Header = new ImageCor20Header(ref cor20HeaderReader, false);
						var version = (uint)(cor20Header.MajorRuntimeVersion << 16) | cor20Header.MinorRuntimeVersion;
						if (version < 0x00020005)
							arch = 32;
						else {
							var bit32Required = (cor20Header.Flags & ComImageFlags.Bit32Required) != 0;
							var bit32Preferred = (cor20Header.Flags & ComImageFlags.Bit32Preferred) != 0;
							var ilOnly = (cor20Header.Flags & ComImageFlags.ILOnly) != 0;
							if (bit32Required)
								arch = 32;
							else if (!bit32Preferred) {
								if (ilOnly)
									arch = Environment.Is64BitOperatingSystem ? 64 : 32;
								else
									arch = 32;
							}
						}
					}
					else
						arch = 32;
				}
			}
			catch {
				return null;
			}

			return arch;
		}
#else
		public static int Main(string[] args) {
			Console.WriteLine("Not supported");
			return -1;
		}
#endif
	}
}
