using System;
using System.Collections.Generic;
using System.IO;

namespace ProtoCore
{
    public class BuildHaltException : Exception
    {
        public string errorMsg { get; private set; }

        public BuildHaltException()
        {
            errorMsg = "Stopping Build\n";
        }

        public BuildHaltException(string message)
        {
            errorMsg = message + '\n';
        }
    }


    namespace BuildData
    {
        public enum WarningID
        {
            kDefault,
            kAccessViolation,
            kCallingConstructorInConstructor,
            kCallingConstructorOnInstance,
            kCallingNonStaticMethodOnClass,
            kFunctionAbnormalExit,
            kFunctionAlreadyDefined,
            kFunctionNotFound,
            kIdUnboundIdentifier,
            kInvalidArguments,
            kInvalidStaticCyclicDependency,
            kInvalidRangeExpression,
            kInvalidThis,
            kMismatchReturnType,
            kMissingReturnStatement,
            kParsing,
            kTypeUndefined,
            kPropertyNotFound,
            kFileNotFound,
            kAlreadyImported,
            kWarnMax
        }

        public struct WarningMessage
        {
            public const string kAssingToThis = "'this' is readonly and cannot be assigned to.";
            public const string kCallingNonStaticProperty = "'{0}.{1}' is not a static property.";
            public const string kCallingNonStaticMethod = "'{0}.{1}()' is not a static method.";
            public const string kMethodHasInvalidArguments = "'{0}()' has some invalid arguments.";
            public const string kInvalidStaticCyclicDependency = "Cyclic dependency detected at '{0}' and '{1}'.";
            public const string KCallingConstructorOnInstance = "Cannot call constructor '{0}()' on instance.";
            public const string kPropertyIsInaccessible = "Property '{0}' is inaccessible.";
            public const string kMethodIsInaccessible = "Method '{0}()' is inaccessible.";
            public const string kCallingConstructorInConstructor = "Cannot call constructor '{0}()' in itself.";
            public const string kPropertyNotFound = "Property '{0}' not found";
            public const string kMethodNotFound = "Method '{0}()' not found";
            public const string kUnboundIdentifierMsg = "Variable '{0}' hasn't been defined yet.";
            public const string kFunctionNotReturnAtAllCodePaths = "Method '{0}()' doesn't return at all code paths.";
            public const string kRangeExpressionWithStepSizeZero = "The step size of range expression should not be 0.";
            public const string kRangeExpressionWithInvalidStepSize = "The step size of range expression is invalid.";
            public const string kRangeExpressionWithNonIntegerStepNumber = "The step number of range expression should be integer.";
            public const string kRangeExpressionWithNegativeStepNumber = "The step number of range expression should be greater than 0.";
            public const string kTypeUndefined = "Type '{0}' is not defined.";
            public const string kMethodAlreadyDefined = "Method '{0}()' is already defined.";
            public const string kReturnTypeUndefined = "Return type '{0}' of method '{1}()' is not defined.";
            public const string kExceptionTypeUndefined = "Exception type '{0}' is not defined.";
            public const string kArgumentTypeUndefined = "Type '{0}' of argument '{1}' is not defined.";
            public const string kInvalidBreakForFunction = "Statement break causes function to abnormally return null.";
            public const string kInvalidContinueForFunction = "Statement continue cause function to abnormally return null.";
            public const string kUsingThisInStaticFunction = "'this' cannot be used in static method.";
            public const string kInvalidThis = "'this' can only be used in member methods.";
            public const string kUsingNonStaticMemberInStaticContext = "'{0}' is not a static property, so cannot be assigned to static properties or used in static methods.";
            public const string kFileNotFound = "File : '{0}' not found";
            public const string kAlreadyImported = "File : '{0}' is already imported";
        }

        public struct ErrorEntry
        {
            public string FileName;
            public string Message;
            public int Line;
            public int Col;
        }

        public struct WarningEntry
        {
            public WarningID id;
            public string msg;
            public int line;
            public int col;
            public string FileName;
        }
    }

    public class OutputMessage
        {
            public enum MessageType { Info, Warning, Error }
            // A constructor for message only for print-out purpose
            public OutputMessage(string message)
            {
                Type = MessageType.Info;
                Message = message;
                FilePath = string.Empty;
                Line = -1;
                Column = -1;
            }
            // A constructor for generic message.
            public OutputMessage(MessageType type, string message)
            {
                Type = type;
                Message = message;
                FilePath = string.Empty;
                Line = -1;
                Column = -1;
            }

            // A constructor for source location related messages.
            public OutputMessage(MessageType type, string message,
                string filePath, int line, int column)
            {
                Type = type;
                Message = message;
                FilePath = filePath;
                Line = line;
                Column = column;
            }

            public MessageType Type { get; private set; }
            public string FilePath { get; private set; }
            public int Line { get; private set; }
            public int Column { get; private set; }
            public string Message { get; private set; }
            public bool Continue { get; set; }

        }

    public interface IOutputStream
    {
        void Write(OutputMessage message);
        List<OutputMessage> GetMessages();
    }

    public class FileOutputStream : IOutputStream
    {
        StreamWriter FileStream { get; set; }

        public FileOutputStream(StreamWriter sw)
        {
            FileStream = sw;
        }

        public void Write(ProtoCore.OutputMessage message)
        {
            if (null == message)
                return;

            if (string.IsNullOrEmpty(message.FilePath))
            {
                // Type: Message
                string formatWithoutFile = "{0}: {1}";
                FileStream.WriteLine(string.Format(formatWithoutFile,
                    message.Type.ToString(), message.Message));
            }
            else
            {
                // Type: Message (File - Line, Column)
                string formatWithFile = "{0}: {1} ({2} - line: {3}, col: {4})";
                FileStream.WriteLine(string.Format(formatWithFile,
                    message.Type.ToString(), message.Message,
                    message.FilePath, message.Line, message.Column));
            }

            if (message.Type == ProtoCore.OutputMessage.MessageType.Warning)
                message.Continue = true;
        }

        public List<ProtoCore.OutputMessage> GetMessages()
        {
            return null;
        }
    }

    public class TextOutputStream : IOutputStream
    {
        public StringWriter TextStream { get; private set; }
        public Dictionary<int, List<string>> Map { get; private set; }

        public TextOutputStream(StringWriter sw, Dictionary<int, List<string>> map)
        {
            TextStream = sw;
            Map = map;
        }

        public TextOutputStream(Dictionary<int, List<string>> map)
        {
            TextStream = new StringWriter();
            Map = map;
        }

        public void Write(ProtoCore.OutputMessage message)
        {
            if (null == message)
                return;

            if (string.IsNullOrEmpty(message.FilePath))
            {
                // Type: Message
                string formatWithoutFile = "{0}: {1}";
                TextStream.WriteLine(string.Format(formatWithoutFile,
                    message.Type.ToString(), message.Message));
            }
            else
            {
                // Type: Message (File - Line, Column)
                string formatWithFile = "{0}: {1} ({2} - line: {3}, col: {4})";
                TextStream.WriteLine(string.Format(formatWithFile,
                    message.Type.ToString(), message.Message,
                    message.FilePath, message.Line, message.Column));
            }

            if (message.Type == ProtoCore.OutputMessage.MessageType.Warning)
                message.Continue = true;
        }

        public List<ProtoCore.OutputMessage> GetMessages()
        {
            return null;
        }
    }

    public class ConsoleOutputStream : IOutputStream
    {
        public ConsoleOutputStream()
        {
        }

        public void Write(ProtoCore.OutputMessage message)
        {
            if (null == message)
                return;

            if (string.IsNullOrEmpty(message.FilePath))
            {
                // Type: Message
                string formatWithoutFile = "{0}: {1}";
                System.Console.WriteLine(string.Format(formatWithoutFile,
                    message.Type.ToString(), message.Message));
            }
            else
            {
                // Type: Message (File - Line, Column)
                string formatWithFile = "{0}: {1} ({2} - line: {3}, col: {4})";
                System.Console.WriteLine(string.Format(formatWithFile,
                    message.Type.ToString(), message.Message,
                    message.FilePath, message.Line, message.Column));
            }

            if (message.Type == ProtoCore.OutputMessage.MessageType.Warning)
                message.Continue = true;
        }

        public List<ProtoCore.OutputMessage> GetMessages()
        {
            return null;
        }
    }

    public class WebOutputStream : IOutputStream
    {
        public ProtoCore.Core core;
        public ProtoLanguage.CompileStateTracker compileState;
        public string filename;
        public WebOutputStream(ProtoCore.Core core)
        {
            this.core = core;
            this.filename = this.core.CurrentDSFileName;
        }
        public WebOutputStream(ProtoLanguage.CompileStateTracker state)
        {
            this.compileState = state;
            this.filename = state.CurrentDSFileName;
        }
        public string GetCurrentFileName()
        {
            return this.filename;
        }

        public void Write(ProtoCore.OutputMessage message)
        {
            if (null == message)
                return;

            if (string.IsNullOrEmpty(message.FilePath))
            {
                // Type: Message
                string formatWithoutFile = "{0}: {1}";

                //System.IO.StreamWriter logFile = new System.IO.StreamWriter("c:\\test.txt");
                if (null != compileState.ExecutionLog)
                {
                    compileState.ExecutionLog.WriteLine(string.Format(formatWithoutFile,
                        message.Type.ToString(), message.Message));
                }

                //logFile.Close();

               // System.Console.WriteLine(string.Format(formatWithoutFile,
                 //   message.Type.ToString(), message.Message));
            }
            else
            {
                // Type: Message (File - Line, Column)
                if (null != compileState.ExecutionLog)
                {
                    string formatWithFile = "{0}: {1} ({2} - line: {3}, col: {4})";
                    compileState.ExecutionLog.WriteLine(string.Format(formatWithFile,
                        message.Type.ToString(), message.Message,
                        message.FilePath, message.Line, message.Column));
                }

                //System.Console.WriteLine(string.Format(formatWithFile,
                  //  message.Type.ToString(), message.Message,
                  //  message.FilePath, message.Line, message.Column));
            }

            if (message.Type == ProtoCore.OutputMessage.MessageType.Warning)
                message.Continue = true;
        }

        public void Close()
        {
            if (null != compileState.ExecutionLog)
                compileState.ExecutionLog.Close();
        }

        public List<ProtoCore.OutputMessage> GetMessages()
        {
            return null;
        }
    }

    public class BuildStatus
    {
        //private Core core;
        private ProtoLanguage.CompileStateTracker compileState;
        private System.IO.TextWriter consoleOut = System.Console.Out;
        private readonly bool LogWarnings = true;
        private readonly bool logErrors = true;
        private readonly bool displayBuildResult = true;
        private readonly bool warningAsError;
        private readonly bool errorAsWarning = false;

        private readonly List<BuildData.WarningEntry> warnings;
        public List<BuildData.WarningEntry> Warnings
        {
            get
            {
                return warnings;
            }
        }
        public IOutputStream MessageHandler { get; set; }
        public WebOutputStream WebMsgHandler { get; set; }

        private readonly List<BuildData.ErrorEntry> errors;
        public List<BuildData.ErrorEntry> Errors
        {
            get
            {
                return errors;
            }
        }
        
        
        public int ErrorCount
        {
            get { return Errors.Count; }
        }

        public int WarningCount
        {
            get { return Warnings.Count; }
        }

      
        public BuildStatus(ProtoLanguage.CompileStateTracker compilestate, bool warningAsError, System.IO.TextWriter writer = null, bool errorAsWarning = false)
        {
            this.compileState = compilestate;
            warnings = new List<BuildData.WarningEntry>();
            
            errors = new List<BuildData.ErrorEntry>();
            this.warningAsError = warningAsError;
            this.errorAsWarning = errorAsWarning;

            if (writer != null)
            {
                consoleOut = System.Console.Out;
                System.Console.SetOut(writer);
            }

            // Create a default console output stream, and this can 
            // be overwritten in IDE by assigning it a different value.
            this.MessageHandler = new ConsoleOutputStream();
            if (compilestate.Options.WebRunner)
            {
                this.WebMsgHandler = new WebOutputStream(compilestate);
            }
        }

      
        public BuildStatus(ProtoLanguage.CompileStateTracker compilestate, bool LogWarnings, bool logErrors, bool displayBuildResult, System.IO.TextWriter writer = null)
        {
            this.compileState = compilestate;
            this.LogWarnings = LogWarnings;
            this.logErrors = logErrors;
            this.displayBuildResult = displayBuildResult;

            //this.errorCount = 0;
            //this.warningCount = 0;
            warnings = new List<BuildData.WarningEntry>();
            
            errors = new List<BuildData.ErrorEntry>();

            if (writer != null)
            {
                consoleOut = System.Console.Out;
                System.Console.SetOut(writer);
            }

            // Create a default console output stream, and this can 
            // be overwritten in IDE by assigning it a different value.
            this.MessageHandler = new ConsoleOutputStream();
        }

        public void SetStream(System.IO.TextWriter writer)
        {
            //  flush the stream first
            System.Console.Out.Flush();

            if (writer != null)
            {
                consoleOut = System.Console.Out;
                System.Console.SetOut(writer);
            }
            else
            {
                System.Console.SetOut(consoleOut);
            }
        }

        
        
        public void LogSyntaxError(string msg, string fileName = null, int line = -1, int col = -1)
        {
            // Error: " + msg + "\n";
            /*if (fileName == null)
            {
                fileName = "N.A.";
            }*/

            if (logErrors)
            {
                var message = string.Format("{0}({1},{2}) Error:{3}", fileName, line, col, msg);
                System.Console.WriteLine(message);
            }

            BuildData.ErrorEntry errorEntry = new BuildData.ErrorEntry
            {
                FileName = fileName,
                Message = msg,
                Line = line,
                Col = col            
            };

            if (compileState.Options.IsDeltaExecution)
            {
                compileState.LogErrorInGlobalMap(ProtoLanguage.CompileStateTracker.ErrorType.Error, msg, fileName, line, col);
            }

            errors.Add(errorEntry);

            OutputMessage outputmessage = new OutputMessage(OutputMessage.MessageType.Error, msg.Trim(), fileName, line, col);
            if (MessageHandler != null)
            {
                MessageHandler.Write(outputmessage);
                if (WebMsgHandler != null)
                {
                    OutputMessage webOutputMsg = new OutputMessage(OutputMessage.MessageType.Error, msg.Trim(), "", line, col);
                    WebMsgHandler.Write(webOutputMsg);
                }
                if (!outputmessage.Continue)
                    throw new BuildHaltException(msg);
            }
        }

        public void LogSemanticError(string msg, string fileName = null, int line = -1, int col = -1, AssociativeGraph.GraphNode graphNode = null)
        {
            if (logErrors)
            {
                System.Console.WriteLine("{0}({1},{2}) Error:{3}", fileName, line, col, msg);
            }

            if (compileState.Options.IsDeltaExecution)
            {
                compileState.LogErrorInGlobalMap(ProtoLanguage.CompileStateTracker.ErrorType.Error, msg, fileName, line, col);
            }

            BuildData.ErrorEntry errorEntry = new BuildData.ErrorEntry
            {
                FileName = fileName,
                Message = msg,
                Line = line,
                Col = col
            };
            errors.Add(errorEntry);

            OutputMessage outputmessage = new OutputMessage(OutputMessage.MessageType.Error, msg.Trim(), fileName, line, col);
            if (MessageHandler != null)
            {
                MessageHandler.Write(outputmessage);
                if (WebMsgHandler != null)
                {
                    OutputMessage webOutputMsg = new OutputMessage(OutputMessage.MessageType.Error, msg.Trim(), "", line, col);
                    WebMsgHandler.Write(webOutputMsg);
                }
                if (!outputmessage.Continue)
                    throw new BuildHaltException(msg);
            }
            throw new BuildHaltException(msg);
        }

        public void LogWarning(BuildData.WarningID warnId, string msg, string fileName = null, int line = -1, int col = -1)
        { 
            //"> Warning: " + msg + "\n"
            /*if (fileName == null)
            {
                fileName = "N.A.";
            }*/

            if (LogWarnings)
            {
                System.Console.WriteLine("{0}({1},{2}) Warning:{3}", fileName, line, col, msg);
            }
            BuildData.WarningEntry warningEntry = new BuildData.WarningEntry { id = warnId, msg = msg, line = line, col = col, FileName = fileName };
            warnings.Add(warningEntry);

            if (compileState.Options.IsDeltaExecution)
            {
                compileState.LogErrorInGlobalMap(ProtoLanguage.CompileStateTracker.ErrorType.Warning, msg, fileName, line, col, warnId);
            }

            OutputMessage outputmessage = new OutputMessage(OutputMessage.MessageType.Warning, msg.Trim(), fileName, line, col);
            if (MessageHandler != null)
            {
                MessageHandler.Write(outputmessage);
                if (WebMsgHandler != null)
                {
                    OutputMessage webOutputMsg = new OutputMessage(OutputMessage.MessageType.Warning, msg.Trim(), "", line, col);
                    WebMsgHandler.Write(webOutputMsg);
                }
                if (!outputmessage.Continue)
                    throw new BuildHaltException(msg);
            }
           
        }

        public bool ContainsWarning(BuildData.WarningID warnId)
        {
            foreach (BuildData.WarningEntry warn in warnings)
            {
                if (warnId == warn.id)
                    return true;
            }
            return false;
        }

        public void ReportBuildResult()
        {
            string buildResult = string.Format("========== Build: {0} error(s), {1} warning(s) ==========\n", errors.Count, warnings.Count);
            if (displayBuildResult)
            {
                System.Console.WriteLine(buildResult);
            }

            if (MessageHandler != null)
            {
                OutputMessage outputMsg = new OutputMessage(OutputMessage.MessageType.Info, buildResult.Trim());
                MessageHandler.Write(outputMsg);
                if (WebMsgHandler != null)
                {
                    WebMsgHandler.Write(outputMsg);
                }
            }
            
        }

        public bool GetBuildResult(out int errcount, out int warncount)
        {
            // TODO Jun: Integrate with the autogen parser
            // The autogen parser whould pass its error and warning results to this class
            //int errorCount = errors.Count;
            //int warningCount = warnings.Count;

            errcount = ErrorCount;
            warncount = WarningCount;

            return (warningAsError) ? (0 == errcount && 0 == warncount) : (0 == errcount);
        }
	}
}

