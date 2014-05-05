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
        Regex expectedIMG = new Regex(@"\\(?<browser>(Chrome|IE9|IE10|Firefox))\\(?<picName>.+).png$");
        public void LoadIndexFile(string indexFile, BindingList<BrowserItem> failedTests)
        {
            var xs = new XmlSerializer(typeof(List<TestResult>), new XmlAttributeOverrides(), new Type[] { typeof(TestResult) }, new XmlRootAttribute("TestResults"), @"");
            var totaltests = new List<TestResult>();
            using (var stream = new StreamReader(indexFile))
            {
                totaltests = xs.Deserialize(stream) as List<TestResult>;
            }
            failedTests.Clear();

            var passedtests = totaltests.Where(test => test.Success).Select(test => test.ExpectedImg);

            var failedtests = from test in totaltests
                              where !test.Success
                              group test by expectedIMG.Match(test.ExpectedImg).Groups["browser"].Value into g
                              select new BrowserItem(g.Key, (from node in g
                                                             select new ErrorItem { 
                                                                 ExpectedImg = node.ExpectedImg, 
                                                                 CapturedImg = node.ResultImg, 
                                                                 Header = expectedIMG.Match(node.ExpectedImg).Groups["picName"].Value,
                                                                 ToolTips = node.CaseName, 
                                                                 Conflict = passedtests.Contains(node.ExpectedImg) }));

            foreach (var item in failedtests)
            {
                failedTests.Add(item);
            }
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
