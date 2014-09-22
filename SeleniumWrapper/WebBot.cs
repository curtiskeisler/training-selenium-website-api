using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;
using System.Diagnostics;
using System.Threading;


// Install-Package Selenium.WebDriver
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;

// Install-Package Selenium.Support
using OpenQA.Selenium.Support.UI;


namespace SeleniumWrapper
{
    public abstract class WebBot : IDisposable
    {

        /// <summary>
        /// The supported browser types
        /// </summary>
        public enum BrowserType { Firefox, Chrome, IE };


        #region Protected Properties
        /// <summary>
        /// The default browswer to use when opening a browswer and
        /// not specifying it.
        /// </summary>
        private BrowserType _DefaultBrowser = BrowserType.Chrome;

        /// <summary>
        /// The default command timeout for browser commands
        /// </summary>
        private TimeSpan _DefaultCommandTimeout = TimeSpan.FromMinutes(2);
        
        /// <summary>
        /// This is the base, Selenium driver used to drive web browser interactions.
        /// All of the commands below are built upon this (i.e. we are Wrapping this).
        /// </summary>
        protected RemoteWebDriver _Driver;

        /// <summary>
        /// They full, physical path where the Selenium Chrome driver can be found.
        /// </summary>
        protected string _ChromeDriverPath;

        /// <summary>
        /// The command timeout for Selenium commands as set in SeleniumDriverCommandTimeoutInSeconds
        /// </summary>
        protected TimeSpan _SeleniumCommandTimeout;

        /// <summary>
        /// This is a collection of information logged by each command as it is performed. This is
        /// extremely helpful to have in a DevOps environment when things fail.
        /// </summary>
        protected StringBuilder _MsgLog = new StringBuilder();

        /// <summary>
        /// This is used for storing screen shots of the browser screen at any time. It is
        /// very helpful to take snapshots when building your API.
        /// </summary>
        protected byte[] _ResultImage;
        public byte[] ResultImage
        {
            get
            {
                if (_ResultImage == null && _Driver != null)
                {
                    try
                    {
                        _ResultImage = ((ITakesScreenshot)_Driver).GetScreenshot().AsByteArray;
                    }
                    catch (Exception ex)
                    {
                        LogMsg("Could not retrieve screenshot. Exception in script: " + ex.ToString());
                    }
                }
                return _ResultImage;
            }
        }
        #endregion

        /// <summary>
        /// Base Constructor
        /// </summary>
        protected WebBot()
        {
            _ChromeDriverPath = ConfigurationManager.AppSettings["ChromeDriverPath"];
            _SeleniumCommandTimeout = TimeSpan.FromSeconds(int.Parse(ConfigurationManager.AppSettings["SeleniumDriverCommandTimeoutInSeconds"]));
        }


        /// <summary>
        /// Dispose of any resources that we have open. This is how you must
        /// get rid of any resources held by the selenium driver.
        /// </summary>
        public virtual void Dispose()
        {
            //Close the browser
            try
            {
                if (_Driver != null)
                {
                    _Driver.Quit();
                }
            }
            catch (Exception ex)
            {
                // eat it
            }
        }


        #region Web Driver Utility Functions

        /// <summary>
        /// Logs the given string to the running log
        /// </summary>
        /// <param name="msg"></param>
        protected void LogMsg(string msg)
        {
            msg = "[" + DateTime.UtcNow.ToString() + "] - " + msg;
            _MsgLog.AppendLine(msg);
            Trace.WriteLine(msg);
        }

        /// <summary>
        /// Logs the given exception to the running error log
        /// </summary>
        /// <param name="exception"></param>
        protected void LogMsg(Exception exception)
        {
            LogMsg("Error: " + exception.ToString());
        }

        /// <summary>
        /// Returns the content of the log so far;
        /// </summary>
        /// <returns></returns>
        protected string GetLog()
        {
            return _MsgLog.ToString();
        }

        /// <summary>
        /// Open a chrome browswer with a default two minute timeout;
        /// </summary>
        protected void OpenBrower()
        {
            OpenBrowser(_DefaultBrowser);
        }

        /// <summary>
        /// Open the given browser type with a default two minute timeout
        /// </summary>
        /// <param name="browser"></param>
        protected void OpenBrowser(BrowserType browser)
        {
            OpenBrowser(browser, _DefaultCommandTimeout );
        }

        /// <summary>
        /// Opens the given browswer type with the given command timeout
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="commandTimeout"></param>
        protected void OpenBrowser(BrowserType browser, TimeSpan commandTimeout)
        {
            LogMsg("OpenBrowser: browser: [" + browser.ToString() + "]");

            const int MAX_RETRY_COUNT = 10;
            // We do progressive backoffs on subsequent attempts so we have 
            // the back off factor a multiple of this value so be careful 
            // to not make it too large
            const int RETRY_PAUSE_MILLISECONDS = 1000;

            // We do a sleep for (RETRY_PAUSE_MILLISECONDS + (retryCount * RETRY_BACKOFF_FACTOR * RETRY_PAUSE_MILLISECONDS))
            const double RETRY_BACKOFF_FACTOR = 0.5;

            int retryCount = 0;
            bool done = false;

            // Loop until we can connect or we time out
            while (!done)
            {
                try
                {
                    switch (browser)
                    {
                        case BrowserType.Firefox:
                            _Driver = new FirefoxDriver();
                            break;

                        case BrowserType.Chrome:
                            ChromeOptions options = new ChromeOptions();
                            options.AddArgument("incognito");
                            _Driver = new ChromeDriver(_ChromeDriverPath, options, _SeleniumCommandTimeout);
                            break;

                        case BrowserType.IE:
                            _Driver = new InternetExplorerDriver();
                            break;

                        default:
                            new Exception("Unhandled browser type given when opening a browser.");
                            break;
                    }

                    // Did we get a driver?
                    done = (_Driver != null);
                    if (done)
                    {
                        _Driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(30));
                    }
                }
                catch (Exception ex)
                {
                    LogMsg("OpenBrowser: An exception occurred opening a [" + browser.ToString() + "]. Here are the details: " + ex.ToString());
                    if ((ex.ToString().Contains("Unable to bind to locking port")) && (retryCount < MAX_RETRY_COUNT))
                    {
                        LogMsg("Unable to bind to a locking port so we are going to retry a few times before giving up.");
                        Thread.Sleep((int)(RETRY_PAUSE_MILLISECONDS + (retryCount * RETRY_BACKOFF_FACTOR * RETRY_PAUSE_MILLISECONDS)));
                        retryCount++;
                    }
                    else
                    {
                        done = true;
                    }
                }
            }

        }


        /// <summary>
        /// Closes the web browser
        /// </summary>
        protected void CloseBrowser()
        {
            _Driver.Close();
        }

        /// <summary>
        /// Navigates to a specific page
        /// </summary>
        /// <param name="url"></param>
        protected bool Navigate(string url)
        {
            LogMsg("Navigate: url: [" + url + "]");
            bool retVal = false;
            try
            {
                _Driver.Navigate().GoToUrl(url);
                retVal = true;
            }
            catch (Exception ex)
            {
                LogMsg("Navigate: Error - " + ex.ToString());
            }
            return retVal;
        }

        /// <summary>
        /// Takes a bitmapped image of the content of the browser. This is very
        /// useful for
        /// </summary>
        /// <returns>A byte array containing the bitmap image</returns>
        protected byte[] TakeScreenShot()
        {
            LogMsg("TakeScreenShot: Say cheese . . .");
            return ((ITakesScreenshot)_Driver).GetScreenshot().AsByteArray;
        }

        /// <summary>
        /// Fills text box with the given name with the given value
        /// </summary>
        /// <param name="name">HTML name of the text box</param>
        /// <param name="value"></param>
        protected void FillTextBox(string name, string value)
        {
            FillTextBox(By.Name(name), value);
        }

        /// <summary>
        /// Fills the textbox defined in the given by with the given value
        /// </summary>
        /// <param name="by"></param>
        /// <param name="value"></param>
        protected void FillTextBox(By by, string value)
        {
            LogMsg("FillTextBox: by: [" + by.ToString() + "] value: [" + value + "]");
            _Driver.FindElement(by).SendKeys(value);
        }


        protected IWebElement FindElement(By by)
        {
            return FindElement(by, 0);
        }

        /// <summary>
        /// Fills the given text box identified using the given By before the timeout expires
        /// </summary>
        /// <param name="by"></param>
        /// <param name="timeoutInSeconds"></param>
        /// <returns></returns>
        protected IWebElement FindElement(By by, int timeoutInSeconds)
        {
            LogMsg("FindElement: by: " + by.ToString() + ", timeoutInSeconds = " + timeoutInSeconds);
            IWebElement element = null;
            try
            {
                if (timeoutInSeconds > 0)
                {
                    var wait = new WebDriverWait(_Driver, TimeSpan.FromSeconds(timeoutInSeconds));
                    element = wait.Until(drv => drv.FindElement(by));
                }
                else
                {
                    element = _Driver.FindElement(by);
                }

            }
            catch (Exception ex)
            {
                LogMsg("FindElement: Exception = " + ex.ToString());
            }
            return element;
        }


        /// <summary>
        /// Click the item identified by the given By clause. This is the base function for the other Click functions
        /// </summary>
        /// <param name="by"></param>
        protected void Click(By by)
        {
            LogMsg("Click: " + by.ToString());
            _Driver.FindElement(by).Click();
        }

        /// <summary>
        /// Clicks the item if found within the given time out. 
        /// </summary>
        /// <param name="by"></param>
        /// <param name="timeoutInSeconds"></param>
        protected void Click(By by, int timeoutInSeconds)
        {
            LogMsg("Click: by = " + by.ToString() + " timeoutInSeconds = " + timeoutInSeconds);
            FindElement(by, timeoutInSeconds).Click();
        }


        /// <summary>
        /// Clicks the element at the given XPath
        /// </summary>
        /// <param name="XPath"></param>
        protected void ClickXPath(string XPath)
        {
            Click(By.XPath(XPath));
        }

        /// <summary>
        /// Return the text in the element defined by the given By
        /// </summary>
        /// <param name="by"></param>
        /// <returns></returns>
        protected string GetText(By by)
        {
            string text = "";
            IWebElement element = FindElement(by);
            if (element != null) text = element.Text;
            return text;
        }

        /// <summary>
        /// Return the text in the element defined by the given xpath
        /// </summary>
        /// <param name="xpath"></param>
        /// <returns></returns>
        protected string GetTextAtXPath(string xpath)
        {
            return GetText(By.XPath(xpath));
        }

        /// <summary>
        /// Waits for the given time in milliseconds
        /// </summary>
        /// <param name="milliseconds"></param>
        protected void Wait(int milliseconds)
        {
            LogMsg("Wait: milliseconds = " + milliseconds);
            Thread.Sleep(milliseconds);
        }

        #endregion


    }
}
