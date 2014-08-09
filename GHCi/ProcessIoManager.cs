using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

 ﻿
/***************************************************************************************
*  Author: Curt C.
*  Email : harpyeaglecp@aol.com
*
*  Project File: "Solving Problems of Monitoring Standard Output and Error Streams Of A Running Process" 
*                for http://www.codeproject.com
*  
*  This software is released under the Code Project Open License (CPOL)
*
*  See official license description at: http://www.codeproject.com/info/cpol10.aspx
*  
* This software is provided AS-IS without any warranty of any kind.
*
* >>> Please leave this header intact when using this file in other projects <<<
***************************************************************************************/

namespace ProcessReadWriteUtils
{
    // Delegate definition used for events to receive notification
    // of text read from stdout or stderr of a running process.
    public delegate void StringReadEventHandler(string text);

    /// <summary>
    /// Class that manages the reading of the output produced by a given 'Process'
    /// and reports the output via events.  
    /// Both standard error (stderr) and standard output (stdout) are
    /// managed and reported.
    /// The stdout and stderr monitoring and reading are each performed
    /// by separate background threads. Each thread blocks on a Read()
    /// method, waiting for text in the stream being monitored to become available.
    /// 
    /// Note the  Process.RedirectStandardOutput must be set to true in order
    /// to read standard output from the process, and the Process.RedirectStandardError
    /// must be set to true to read standard error.
    /// </summary>
    public class ProcessIoManager
    {
        #region Private_Fields
        // Command line process that is executing and being
        // monitored by this class for stdout/stderr output.
        private Process runningProcess;

        // Thread to monitor and read standard output (stdout)
        private Thread stdoutThread;

        // Thread to monitor and read standard error (stderr)
        private Thread stderrThread;

        // Buffer to hold characters read from either stdout, or stderr streams
        private StringBuilder streambuffer;
        #endregion

        #region Public_Properties_And_Events
        /// <summary>
        /// Gets the process being monitored
        /// </summary>
        /// <value>The running process.</value>
        public Process RunningProcess
        {
            get { return runningProcess; }
        }

        // Event to notify of a string read from stdout stream
        public event StringReadEventHandler StdoutTextRead;

        // Event to notify of a string read from stderr stream
        public event StringReadEventHandler StderrTextRead;

        #endregion

        #region Constructor_And_Initialization
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessIoManager"/> class.        
        /// </summary>
        /// <param name="process">The process.</param>
        /// <remarks>
        /// Does not automatically start listening for stdout/stderr.
        /// Call StartProcessOutputRead() to begin listening for process output.
        /// </remarks>
        /// <seealso cref="StartProcessOutputRead"/>
        public ProcessIoManager(Process process)
        {            
            if (process == null)
                throw new Exception("ProcessIoManager unable to set running process - null value is supplied");

            if (process.HasExited == true)
                throw new Exception("ProcessIoManager unable to set running process - the process has already existed.");

            this.runningProcess = process;
            this.streambuffer = new StringBuilder(256);
        }
        #endregion

        #region Public_Methods
        /// <summary>
        /// Starts the background threads reading any output produced (standard output, standard error)
        /// that is produces by the running process.
        /// </summary>
        public void StartProcessOutputRead()
        {
            // Just to make sure there aren't previous threads running.
            StopMonitoringProcessOutput();

            // Make sure we have a valid, running process
            CheckForValidProcess("Unable to start monitoring process output.", true);

            // If the stdout is redirected for the process, then start
            // the stdout thread, which will manage the reading of stdout from the
            // running process, and report text read via supplied events.
            if (runningProcess.StartInfo.RedirectStandardOutput == true)
            {
                stdoutThread = new Thread(new ThreadStart(ReadStandardOutputThreadMethod));
                // Make thread a background thread - if it was foreground, then
                // the thread could hang up the process from exiting. Background
                // threads will be forced to stop on main thread termination.
                stdoutThread.IsBackground = true;
                stdoutThread.Start();
            }

            // If the stderr  is redirected for the process, then start
            // the stderr thread, which will manage the reading of stderr from the
            // running process, and report text read via supplied events.
            if (runningProcess.StartInfo.RedirectStandardError == true)
            {
                stderrThread = new Thread(new ThreadStart(ReadStandardErrorThreadMethod));
                stderrThread.IsBackground = true;
                stderrThread.Start();
            }
        }

        /// <summary>
        /// Writes the supplied text string to the standard input (stdin) of the running process
        /// </summary>
        /// <remarks>In order to be able to write to the Process, the StartInfo.RedirectStandardInput must be set to true.</remarks>
        /// <param name="text">The text to write to running process input stream.</param>
        public void WriteStdin(string text)
        {
            // Make sure we have a valid, running process
            CheckForValidProcess("Unable to write to process standard input.", true);
            if (runningProcess.StartInfo.RedirectStandardInput == true)
                runningProcess.StandardInput.WriteLine(text);
        }
        #endregion

        #region Private_Methods
        /// <summary>
        /// Checks for valid (non-null Process), and optionally check to see if the process has exited.
        /// Throws Exception if process is null, or if process has existed and checkForHasExited is true.
        /// </summary>
        /// <param name="errorMessageText">The error message text to display if an exception is thrown.</param>
        /// <param name="checkForHasExited">if set to <c>true</c> [check if process has exited].</param>
        private void CheckForValidProcess(string errorMessageText, bool checkForHasExited)
        {
            errorMessageText = (errorMessageText==null?"":errorMessageText.Trim());
            if(runningProcess==null)
                throw new Exception(errorMessageText + " (Running process must be available)");

            if(checkForHasExited && runningProcess.HasExited)
                throw new Exception(errorMessageText + " (Process has exited)");
        }

        /// <summary>
        /// Read characters from the supplied stream, and accumulate them in the
        /// 'streambuffer' variable until there are no more characters to read.
        /// </summary>
        /// <param name="firstCharRead">The first character that has already been read.</param>
        /// <param name="streamReader">The stream reader to read text from.</param>
        /// <param name="isstdout">if set to <c>true</c> the stream is assumed to be standard output, otherwise assumed to be standard error.</param>
        private void ReadStream(int firstCharRead, StreamReader streamReader, bool isstdout)
        {
            // One of the streams (stdout, stderr) has characters ready to be written
            // Flush the supplied stream until no more characters remain to be read.
            // The synchronized/ locked section of code to prevent the other thread from
            // reading its stream at the same time, producing intermixed stderr/stdout results. 
            // If the threads were not synchronized, the threads
            // could read from both stream simultaneously, and jumble up the text with
            // stderr and stdout text intermixed.
            lock (this)
            {
                // Single character read from either stdout or stderr
                int ch;
                // Clear the stream buffer to hold the text to be read
                streambuffer.Length = 0;

                //Console.WriteLine("CHAR=" + firstCharRead);
                streambuffer.Append((char)firstCharRead);

                // While there are more characters to be read
                while (streamReader.Peek() > -1)
                {
                    // Read the character in the queue
                    ch = streamReader.Read();

                    // Accumulate the characters read in the stream buffer
                    streambuffer.Append((char)ch);

                    // Send text one line at a time - much more efficient than
                    // one character at a time
                    if (ch == '\n')
                        NotifyAndFlushBufferText(streambuffer, isstdout);
                }
                // Flush any remaining text in the buffer
                NotifyAndFlushBufferText(streambuffer, isstdout);
            } // End lock()
        }

        /// <summary>
        /// Invokes the OnStdoutTextRead (if isstdout==true)/ OnStderrTextRead events
        /// with the supplied streambuilder 'textbuffer', then clears
        /// textbuffer after event is invoked.
        /// </summary>
        /// <param name="textbuffer">The textbuffer containing the text string to pass to events.</param>
        /// <param name="isstdout">if set to true, the stdout event is invoked, otherwise stedrr event is invoked.</param>
        private void NotifyAndFlushBufferText(StringBuilder textbuffer, bool isstdout)
        {
            if (textbuffer.Length > 0)
            {
                if (isstdout == true && StdoutTextRead != null)
                {   // Send notificatin of text read from stdout
                    StdoutTextRead(textbuffer.ToString());
                }
                else if (isstdout == false && StderrTextRead != null)
                {   // Send notificatin of text read from stderr
                    StderrTextRead(textbuffer.ToString());
                }
                // 'Clear' the text buffer
                textbuffer.Length = 0;
            }
        }

        /// <summary>
        /// Method started in a background thread (stdoutThread) to manage the reading and reporting of
        /// standard output text produced by the running process.
        /// </summary>
        private void ReadStandardOutputThreadMethod()
        {
            // Main entry point for thread - make sure the main entry method
            // is surrounded with try catch, so an uncaught exception won't
            // screw up the entire application
            try
            {
                // character read from stdout
                int ch;

                // The Read() method will block until something is available
                while (runningProcess != null && (ch = runningProcess.StandardOutput.Read()) > -1)
                {
                    // a character has become available for reading
                    // block the other thread and process this stream's input.
                    ReadStream(ch, runningProcess.StandardOutput, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ProcessIoManager.ReadStandardOutputThreadMethod()- Exception caught:" +
                    ex.Message + "\nStack Trace:" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Method started in a background thread (stderrThread) to manage the reading and reporting of
        /// standard error text produced by the running process.
        /// </summary>
        private void ReadStandardErrorThreadMethod()
        {
            try
            {
                // Character read from stderr
                int ch;
                // The Read() method will block until something is available
                while (runningProcess != null && (ch = runningProcess.StandardError.Read()) > -1)
                    ReadStream(ch, runningProcess.StandardError, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ProcessIoManager.ReadStandardErrorThreadMethod()- Exception caught:" +
                     ex.Message + "\nStack Trace:" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Stops both the standard input and stardard error background reader threads (via the Abort() method)        
        /// </summary>
        public void StopMonitoringProcessOutput()
        {
            // Stop the stdout reader thread
            try
            {
                if (stdoutThread != null)
                    stdoutThread.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ProcessIoManager.StopReadThreads()-Exception caught on stopping stdout thread.\n" +
                    "Exception Message:\n" + ex.Message + "\nStack Trace:\n" + ex.StackTrace);
            }

            // Stop the stderr reader thread
            try
            {
                if (stderrThread != null)
                    stderrThread.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ProcessIoManager.StopReadThreads()-Exception caught on stopping stderr thread.\n" +
                    "Exception Message:\n" + ex.Message + "\nStack Trace:\n" + ex.StackTrace);
            }
            stdoutThread = null;
            stderrThread = null;
        }
        #endregion
    }
}