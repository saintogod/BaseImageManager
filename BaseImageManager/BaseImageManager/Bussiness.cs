using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace BaseImageManager
{
    internal class Bussiness
    {
        static double fuzzyThreshold = 30;
        Regex expectedIMG = new Regex(@"\\(?<browser>(Chrome|IE9|IE10|Firefox))\\(?<picName>.+).png$");
        const StringComparison I = StringComparison.InvariantCultureIgnoreCase;

        public void LoadIndexFile(string indexFile, BindingList<BrowserItem> failedTests)
        {
            var xs = new XmlSerializer(typeof(List<TestResult>), new XmlAttributeOverrides(), new Type[] { typeof(TestResult) }, new XmlRootAttribute("TestResults"), @"");
            var totaltests = new List<TestResult>();
            using (var stream = new StreamReader(indexFile))
            {
                totaltests = xs.Deserialize(stream) as List<TestResult>;
            }
            failedTests.Clear();

            var passedTests = totaltests.Where(test => test.Success);

            var failedtests = from test in totaltests
                              where !test.Success
                              group test by expectedIMG.Match(test.ExpectedImg).Groups["browser"].Value into g
                              select new BrowserItem(g.Key, (from node in g
                                                             select new ErrorItem
                                                             {
                                                                 ExpectedImg = node.ExpectedImg,
                                                                 CapturedImg = node.ResultImg,
                                                                 Header = expectedIMG.Match(node.ExpectedImg).Groups["picName"].Value,
                                                                 ToolTips = node.CaseName,
                                                                 ConflictWith = passedTests.Where(item => item.ExpectedImg.Equals(node.ExpectedImg, I))
                                                                                            .Select(item => item.CaseName)
                                                                                            .FirstOrDefault()
                                                             }));

            foreach (var item in failedtests)
            {
                failedTests.Add(item);
            }
        }

        private bool CompareImage(string img, string imgToCompare, out string diff)
        {
            diff = Path.Combine(Path.GetDirectoryName(img), "diff_between_tests.png");
            using (Process p = new Process())
            {
                ProcessStartInfo ps = new ProcessStartInfo("cmd.exe", string.Format("/c compare -metric AE -fuzz {0}% \"{1}\" \"{2}\" \"{3}\"", fuzzyThreshold, imgToCompare, img, diff));
                ps.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo = ps;
                p.Start();
                if (p.WaitForExit(60000))
                    return p.ExitCode == 0;
            }
            return false;
        }

        public bool CheckConflict( BindingList<BrowserItem> failedTests, out string messages)
        {
            var messageBuilder = new StringBuilder();
            var dup = failedTests.SelectMany(browser => browser.ErrorItems.Where(item => item.IsChecked))
                .GroupBy(item => item.ExpectedImg)
                .Where(kv => kv.Count() > 0);
            var hasConflict = false;
            foreach (var item in dup)
            {
                var list = item.ToArray();
                for (int i = 0; i < list.Length; i++)
                {
                    for (int j = i + 1; j < list.Length; j++)
                    {
                        var diffImg = "";
                        if (!CompareImage(list[i].CapturedImg, list[j].CapturedImg, out diffImg))
                        {
                            list[i].IsChecked = false;
                            list[j].IsChecked = false;
                            hasConflict = true;
                            messageBuilder.AppendFormat("  [Conflict]: Result of {0} and {1} are conflict, see {2} vs {3} => {4}\r\n", list[i].ToolTips, list[j].ToolTips, list[i].CapturedImg, list[j].CapturedImg, diffImg);
                        }
                    }
                }
            }
            if (hasConflict)
                messageBuilder.Insert(0, "Conflict Between Failed Tests:\r\n");
            var conflictWithPassed = failedTests.SelectMany(browser => browser.ErrorItems.Where(item => item.Conflict));
            if (conflictWithPassed.Count() > 0)
            {
                messageBuilder.AppendLine("------------------------");
                messageBuilder.AppendLine("Conflict Between Passed TestCase and Failed TestCase: (the following images can't be applied, please check your code first)");
            }
            foreach (var item in conflictWithPassed)
            {
                messageBuilder.AppendFormat("  [Error]: Case {0} passed while case {1} failed, while the used same pic {2}\r\n", item.ConflictWith, item.ToolTips, item.ExpectedImg);
            }
            messages = messageBuilder.ToString();
            return hasConflict;
        }

        public string ApplyImages(BindingList<BrowserItem> failedTests)
        {
            var testToUpdate = failedTests.SelectMany(browser => browser.ErrorItems.Where(item => item.IsChecked));
            int failedToUpdate = 0;
            StringBuilder msg = new StringBuilder();
            foreach (var item in testToUpdate)
            {
                try
                {
                    File.Copy(item.CapturedImg, item.ExpectedImg, true);
                }
                catch (Exception ex)
                {
                    msg.AppendFormat("Failed to copy {0} to {1} [Exception: {2}]\r\n", item.CapturedImg, item.ExpectedImg, ex.Message);
                    failedToUpdate++;
                }
            }
            msg.AppendFormat("\r\nTotally upated {0} Images, failed to update {1} images.", testToUpdate.Count(), failedToUpdate);
            return msg.ToString();
        }

        public void CommitToSVN(string svnFolder)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo()
                {
                    FileName = "TortoiseProc.exe",
                    Arguments = string.Format(@"/command:commit /path:""{0}""", svnFolder)
                };
                process.Start();
            }
        }

        public void OpenImage(string imagePath)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo() { FileName = imagePath };
                process.Start();
            }
        }

        public void ViewInSvnDiff( string capturedImage,string expectedImage)
        {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo()
                    {
                        FileName = "tortoiseidiff",
                        Arguments = string.Format(@"/left:""{0}"" /lefttitle:""Captured Image"" /right:""{1}"" /righttitle:""Expected Image"" /fit /overlay", capturedImage, expectedImage)
                    };
                    process.Start();
                }
        }

    }
}
