using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Mail;

using System.Linq;

// Wrappers, shortcuts and simplifiers here only.
// No big functions, no complicated logic.

// TODO: split?

namespace QAS
{
    public static class Utils
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes
        );

        public static string Today()
        {
            DateTime now = DateTime.Now;
            return now.Month.ToString().PadLeft(2, '0') + "/" + now.Day.ToString().PadLeft(2, '0') + "/" + now.Year.ToString();
        }

        public static string Now(bool humanRead = false)
        {
            DateTime now = DateTime.Now;
            return string.Format( (humanRead ? "{0}.{1:D2}.{2:D2} {3:D2}:{4:D2}:{5:D2}" : "{0}{1:D2}{2:D2}.{3:D2}{4:D2}{5:D2}"), now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
        }

        public static string JoinToString(params object[] values)
        {
            return string.Join("", values.ToArray().Select(x => x != null ? x.ToString() : "[NULL]")); 
        }

        public static bool EchoTime = true;

        public static void Echo(params object[] values)
        {
            Trace.WriteLine((EchoTime ? Now(true) +" " : "") + JoinToString(values));
        }


        public static void ConsoleWriteWithColor(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }


        public static void WriteLine(params object[] values)
        {
            Console.WriteLine(JoinToString(values));
        }

        public static void Warn(params object[] values) 
        {
            Echo("[WARNING] ", JoinToString(values));
        }

        public static void Line(string message = null)
        {
            string delimiter = "-------------------------------------------------------------------------";
            if(message != null)
            {
                int m = message.Length;
                int d = delimiter.Length;
                delimiter = new StringBuilder(delimiter).Remove((d - m) / 2, m).Insert((d - m) / 2, message).ToString();
            }
            Echo(delimiter);
        }

        public static bool Verbose = false;
        public static void Display(string s, ConsoleColor? fore = null, ConsoleColor? back = null)
        {
            if (!Verbose)
            {
                return;
            }
            ConsoleColor oldfore = Console.ForegroundColor, oldback = Console.BackgroundColor;

            Console.ForegroundColor = fore.HasValue ? fore.Value : oldfore;
            Console.BackgroundColor = back.HasValue ? back.Value : oldback;

            int leftToRead = Console.CursorLeft, top = Console.CursorTop;
            Console.Write(s);
            Console.SetCursorPosition(leftToRead, top);

            Console.ForegroundColor = oldfore;
            Console.BackgroundColor = oldback;
        }
        
        public static void RemoveDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    foreach (string folder in Directory.GetDirectories(path))
                    {
                        RemoveDirectory(folder);
                    }
                    foreach (string file in Directory.GetFiles(path))
                    {
                        RemoveFile(file);
                    }
                    Directory.Delete(path, true);
                }
                catch (Exception e)
                {
                    Warn("Failed to remove " + path + " because " + e.Message);
                }
            }
        }

        public static string CreateDirectory(string path, bool cleanup = false)
        {
            try
            {
                if (cleanup && Directory.Exists(path))
                {
                    RemoveDirectory(path);
                }
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception e)
            {
                Warn("Exception while creating directory " + path + ": " + e.Message);
            }
            return path;
        }

        public static void RemoveFile(string file)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    Warn("Exception while removing file " + file + ": " + e.Message);
                }
            }
        }

        public static void Cp(IEnumerable<string> srcFiles, string dstDir, bool tryLink = false)
        {
            foreach (string srcFile in srcFiles)
            {
                string dstFile = Path.Combine(dstDir, Path.GetFileName(srcFile));
                RemoveFile(dstFile);
                Cp(srcFile, dstFile, tryLink);
            }
        }

        public static void Cp(string srcFile, string dstFile, bool tryLink = false)
        {
            if (tryLink)
            {
                CreateHardLink(dstFile, srcFile, IntPtr.Zero);
                if (!File.Exists(dstFile))
                {
                    File.Copy(srcFile, dstFile);
                }
            }
            else
            {
                File.Copy(srcFile, dstFile);
            }
        }

        public static IEnumerable<string> Cat(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new ApplicationException("File not found: " + fileName);
            }

            var res = new List<string>();
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var sr = new StreamReader(fs))
                {
                    // I have to add this ugly hack because our configurations allow standalone '\r' which are
                    // considered valid EOL by some programs like far or notepad, but sdp pack thinks it is just a character
                    // TODO: Prohibit them in QCS configurations
                    res = sr.ReadToEnd().Split(new string[]{"\r\n"}, StringSplitOptions.None).ToList();
                    if (res.Last() == "")
                    {
                        res.RemoveAt(res.Count - 1); // remove last empty line
                    }
                }
            }
            return res;
        }

        public static bool FilesAreEqual(string file1, string file2)
        {
            var list1 = Utils.Cat(file1).ToList();
            var list2 = Utils.Cat(file2).ToList();

            if (list1.Count != list2.Count)
            {
                return false;
            }
            for (int i = 0; i < list1.Count; ++i)
            {
                if (list1[i] != list2[i])
                {
                    return false;
                }
            }
            return true;
        }

        // -------------------------------------------------------------
        // Regex extensions for string
        // Returns success/fail boolean, fills list of strings with matches
        public static bool Match(this string input, string pattern, out List<string> matches)
        {
            Match m = new Regex(pattern).Match(input);
            matches = new List<string>();
            if (m.Success)
            {
                matches = m.Groups.Cast<Group>().Select(x => x.Value).ToList();
            }
            return m.Success;
        }

        public static IEnumerable<List<string>> MultiMatch(this string input, string pattern)
        {
            Match m = new Regex(pattern).Match(input);
          
            while (m.Success) {
                yield return m.Groups.Cast<Group>().Select(x => x.Value).ToList();
                m = m.NextMatch();
            }
        }

        public static bool Match(this string input, string pattern)
        {
            List<string> matches;
            return Match(input, pattern, out matches);
        }

        public static string RegexReplace(this string input, string pattern, string replacement)
        {
            return new Regex(pattern).Replace(input, replacement);
        }

        // -------------------------------------------------------------

        public class CommandLine : List<string>
        {
            public struct Arg
            {
                public delegate void Setter(string value);
                public int i;
                public string key;
                public Setter argSetter;
                public string defValue;
                public string description;
            };

            Dictionary<string, Arg> Opts = new Dictionary<string, Arg>();

            public void Add(string description, string key = null, Arg.Setter argSetter = null, string defValue = null)
            {
                key = (key == null) ? "DELIMITER " + Opts.Count.ToString() : "--" + key.ToLower();
                Opts.Add(key, new Arg() { i = Opts.Count, key = key, argSetter = argSetter, defValue = defValue, description = description });
            } 

            public void Parse(string[] args)
            {
                Add("This screen", "help", (v) => {Help();});

                // Setting default values
                foreach (var arg in Opts.Values.Where(x => x.defValue != null))
                {
                    arg.argSetter(arg.defValue);
                }

                for (int i = 0; i < args.Count(); )
                {
                    string key = args[i].ToLower();
                    if (!Opts.ContainsKey(key))
                    {
                        Console.WriteLine("Argument " + key + " is not defined. Use --help option for help");
                        Environment.Exit(1);
                    }

                    string value = args.Count() > i + 1 ? args[i + 1] : "";
                    try
                    {
                        Opts[key].argSetter(value); // TODO: flag options without values
                    }
                    catch (ValidationFail e)
                    {
                        StopIf(true, String.Format("command line parsing: " + e.Message, key, value, Opts[key].description));
                    }
                    i += 2;
                    continue;
                }
            }

            public void Help()
            {
                foreach (var arg in this.Opts.Values.OrderBy(arg => arg.i))
                {
                    Console.WriteLine(arg.key.StartsWith("DELIMITER") ? arg.description : "  " + arg.key + ":\t" + arg.description);
                    if (arg.defValue != null)
                    {
                        Console.WriteLine("    (default: " + arg.defValue + ")");
                    }
                }
                Environment.Exit(0);
            }
        };

        // Http:
        public class Http 
        {
            HttpWebRequest req;

            public virtual string Url { get; set; }

            // Todo: remove url from parameters here
            public static Http SendRequest(string url, int timeout = 0)
            {
                Http self = new Http() { Url = url };

                try
                {
                    self.req = (HttpWebRequest)WebRequest.Create(url);
                    self.req.KeepAlive = false;
                    self.req.Proxy = null;
                    self.req.ServicePoint.ConnectionLeaseTimeout = 0;
                    self.req.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                    self.req.Credentials = CredentialCache.DefaultCredentials;
                    self.req.CookieContainer = new CookieContainer();

                    if (timeout > 0)
                    {
                        self.req.Timeout = timeout;
                    }
                }
                catch (Exception)
                {
                    Utils.Echo("Error while sending http request: ", url);
                    throw;
                }

                return self;
            }

            public List<string> GetTextResponse(int retries = 10, int delay = 5000)
            {
                var result = new List<string>();
                try
                {
                    using (StreamReader sr = new StreamReader(req.GetResponse().GetResponseStream()))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            result.Add(line);
                        }
                    }
                }
                catch (Exception e)
                {
                    Warn("Error when getting http response from ", req.RequestUri, ", error: ", e.Message);
                    throw;
                }
                return result;
            }

            public delegate void RespReader(byte[] buffer, int length, long leftToRead);

            public void GetBinaryResponse(RespReader callBack, int bufSize = 4096 * 8)
            {
                byte[] buffer = new byte[bufSize];
                try
                {
                    using (Stream sr = req.GetResponse().GetResponseStream())
                    {
                        long bytesToRead = req.GetResponse().ContentLength;
                        while (bytesToRead > 0)
                        {
                            int length = sr.Read(buffer, 0, bufSize);
                            if (length == 0)
                                break;
                            bytesToRead -= length;

                            callBack(buffer, length, bytesToRead);
                        }
                    }
                }
                catch (Exception e)
                {
                    Warn("Error when getting http response from " + req.RequestUri + ", error: " + e.Message);
                    throw;
                }
            }
        }

        // Exception for convenient stop of validation at any step
        public class ValidationFail : Exception
        {
            public ValidationFail(string message) : base(message)
            {
            }
        }

        public static void StopIf(bool condition, params object[] values)
        {
            if (condition)
            {
                throw new ValidationFail(JoinToString(values));
            }
        }

        public static void ErrorIf(bool condition, params object[] values) 
        {
            if (condition)
            {
                throw new ApplicationException(JoinToString(values));
            }
        }

        //
        // Helper class for synchronizing multiple processes using exclusive lock on the same file
        //
        public class FileLocker
        {
            readonly string Name;
            FileStream lockFile = null;

            public FileLocker(string name)
            {
                Name = name;
                if (!File.Exists(Name))
                {
                    File.Create(Name);
                }
            }

            // Blocks until file is successfully locked
            public void WaitAndLock()
            {
                if (lockFile == null)
                {
                    while (true)
                    {
                        try
                        {
                            lockFile = File.Open(Name, FileMode.OpenOrCreate, FileAccess.Write);
                            return;
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
            }

            public void ReleaseLock()
            {
                if (lockFile != null)
                {
                    lockFile.Close();
                    lockFile = null;
                }
            }
        }

        //
        // Helper class for sending emails
        //
        public class Email
        {
            MailMessage Msg = new MailMessage();

            public Email() { }
            public Email From(string addr) { Msg.From = new MailAddress(addr); return this; }
            public Email To(string addr) { Msg.To.Add(addr); return this; }
            public Email CC(string addr) { Msg.CC.Add(addr); return this; }

            public Email Subject(string subj) { Msg.Subject = subj; return this; }
            public Email Body(string bodyHtml)
            {
                Msg.IsBodyHtml = true;
                Msg.Body = bodyHtml;
                return this;
            }
            public Email Attach(string fileName, string mediaType = @"text/htm")
            {
                Msg.Attachments.Add(new Attachment(fileName, mediaType));
                return this;
            }

            public Email AttachFromString(string content, string name = "attachment.htm", string mediaType = @"text/htm")
            {
                Msg.Attachments.Add(new Attachment(new MemoryStream(Encoding.UTF8.GetBytes(content)), name, mediaType));
                return this;
            }

            public Exception Exception { get; private set; }

            public bool Send(string server = "smtphost.redmond.corp.microsoft.com", int port = 25, ICredentialsByHost credentials = null)
            {
                try
                {
                    SmtpClient smtpClient = new SmtpClient(server, port) { UseDefaultCredentials = (credentials == null) };
                    if (!smtpClient.UseDefaultCredentials)
                    {
                        smtpClient.Credentials = credentials; 
                        smtpClient.EnableSsl = true;
                    }

                    smtpClient.Send(Msg);
                }
                catch (Exception e)
                {
                    this.Exception = e;
                    return false;
                }
                return true;
            }
        }

        // ---------------------------------------------------------------------------------------------
        // List helpers: split string into list, join list element to string
        public static IEnumerable<string> AsList(this string str, params string[] delimiters)
        {
            delimiters = delimiters.Count() == 0 ? new string[] { ";", "," } : delimiters;
            return str.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string AsString<T>(this IEnumerable<T> list, string delim = ";")
        {
            return string.Join(delim, list.Select(e => e.ToString()));
        }


    }
}

