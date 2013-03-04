using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Mail;

namespace C_Sharp
{
    public class PostCommit
    {
        private static string svnpath = Environment.GetEnvironmentVariable("VISUALSVN_SERVER");
        private static int Main(string[] args)
        {
            //Check if revision number and revision path have been supplied.
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Invalid arguments sent - <REPOSITORY> <REV> required");
                return 1;
            }

            //Check if VisualSVN is installed.
            if (string.IsNullOrEmpty(svnpath))
            {
                Console.Error.WriteLine("VISUALSVN_SERVER environment variable does not exist. VisualSVN installed?");
                return 1;
            }

            //Get the required information using SVNLook.
            string author = SVNLook("author", args);
            string message = SVNLook("log", args);
            string changed = SVNLook("changed", args);

            //Get the branch from the first change in the list.
            string[] changeList = changed.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            string changeFirst = changeList[0].Remove(0, 4);
            int changeFirstSlash = changeFirst.IndexOf("/");
            string repoBranch = changeFirst.Substring(0, changeFirstSlash);

            //Get the name of the repository from the first argument, which is the repo path.
            string repoName = args[0].ToString().Substring(args[0].LastIndexOf(@"\") + 1 );

            //Get the email template and fill it in. This template can be anywhere, and can be a .HTML file
            //for more control over the structure.
            string emailTemplatePath = @"C:\hooks\PostCommit.txt";
            string emailTemplate = string.Format(File.ReadAllText(emailTemplatePath), author, message, changed);

            //Construct the email that will be sent. You can use the .IsBodyHtml property if you are
            //using an HTML template.
            string subject = string.Format("commit number {0} for {1}", args[1], repoName);
            MailMessage mm = new MailMessage("<from email>", "<to email>");
            mm.Body = emailTemplate;
            mm.Subject = subject;

            //Define your mail client. I am using Gmail here as the SMTP server, but you could
            //use IIS or Amazon SES or whatever you want.
            SmtpClient mailClient = new SmtpClient("smtp.gmail.com");
            mailClient.Port = 587;
            mailClient.Credentials = new System.Net.NetworkCredential("<gmail username>", "<gmail password>");
            mailClient.EnableSsl = true;

            mailClient.Send(mm);

            return 0;
        }

        /// <summary>
        /// Runs a command on svnlook.exe to get information
        /// about a particular repo and revision.
        /// </summary>
        /// <param name="command">The svnlook command e.g. log, author, message.</param>
        /// <param name="args">The arguments passed in to this exe (repo name and rev number).</param>
        /// <returns>The output of svnlook.exe</returns>
        private static string SVNLook(string command, string[] args) {
            StringBuilder output = new StringBuilder();
            Process procMessage = new Process();

            //Start svnlook.exe in a process and pass it the required command-line args.
            procMessage.StartInfo = new ProcessStartInfo(svnpath + @"bin\svnlook.exe", String.Format(@"{0} ""{1}"" -r ""{2}""", command, args[0], args[1]));
            procMessage.StartInfo.RedirectStandardOutput = true;
            procMessage.StartInfo.UseShellExecute = false;
            procMessage.Start();

            //While reading the output of svnlook, append it to the stringbuilder then
            //return the output.
            while (!procMessage.HasExited)
            {
                output.Append(procMessage.StandardOutput.ReadToEnd());
            }

            return output.ToString();
        }
    }
}
