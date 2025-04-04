//-----------------------------------------------------------------------
// <copyright file="ComplexConfig.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Text;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.Listeners.RequestListenerEventArgs;
using TcHmiSrv.Core.Tools.Management;

namespace ComplexConfig
{
    // ReSharper disable once UnusedType.Global
    public class ComplexConfig : IServerExtension
    {
        private readonly RequestListener _requestListener = new RequestListener();

        public ErrorValue Init()
        {
            // add event handlers
            _requestListener.OnRequest += OnRequest;

            return ErrorValue.HMI_SUCCESS;
        }

        private void OnRequest(object sender, OnRequestEventArgs e)
        {
            // handle all commands one by one
            foreach (var command in e.Commands)
            {
                try
                {
                    switch (command.Mapping)
                    {
                        case "MarkAsDone":
                            MarkAsDone(command);
                            break;
                        case "CreateReport":
                            CreateReport(command);
                            break;
                    }
                }
                catch
                {
                    // ignore exceptions and continue processing the other commands in the group
                    command.ExtensionResult = Convert.ToUInt32(ExtensionSpecificError.InternalError);
                }
            }
        }

        private void MarkAsDone(Command command)
        {
            string project = command.WriteValue["project"];
            int index = command.WriteValue["index"];

            // find out how many items are currently in the 'done' array.
            // when the 'limit' paging parameter is used, the command response contains
            // the 'maxEntries' field that indicates how many items exist.
            var countCommand = new Command(TcHmiApplication.JoinPath(TcHmiApplication.Context.Domain + ".Config", DonePath(project)))
            {
                Paging = new Value
                {
                    { "limit", 0 }
                }
            };
            var adminContext = TcHmiApplication.Context;
            var error = TcHmiApplication.AsyncHost.Execute(ref adminContext, ref countCommand);
            if (error != ErrorValue.HMI_SUCCESS)
            {
                command.ExtensionResult = (uint)ExtensionSpecificError.CountCommandFailed;
                return;
            }

            int nextIndex = countCommand.Paging["maxEntries"];

            // move the 'todo' item to the end of the 'done' array
            error = TcHmiApplication.AsyncHost.RenameConfigValue(TcHmiApplication.Context, ToDoPath(project, index), DonePath(project, nextIndex));
            if (error != ErrorValue.HMI_SUCCESS)
            {
                command.ExtensionResult = (uint)ExtensionSpecificError.ConfigurationChangeRejected;
            }
        }

        private void CreateReport(Command command)
        {
            string project = command.WriteValue["project"];

            var doneItems = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, DonePath(project));
            var allReports = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, ReportsPath(project));
            if (!allReports.IsVector || !doneItems.IsVector)
            {
                command.ExtensionResult = (uint)ExtensionSpecificError.InternalError;
                return;
            }

            // create the new report, add it to the array of reports and write the whole array back to the extension configuration
            allReports.Add(ItemsToMarkdown(doneItems));
            var error = TcHmiApplication.AsyncHost.ReplaceConfigValue(TcHmiApplication.Context, ReportsPath(project), allReports);
            if (error != ErrorValue.HMI_SUCCESS)
            {
                command.ExtensionResult = (uint)ExtensionSpecificError.ConfigurationChangeRejected;
                return;
            }

            // replace the 'done' array in the configuration with an empty array
            error = TcHmiApplication.AsyncHost.ReplaceConfigValue(TcHmiApplication.Context, DonePath(project), new Value { Type = TcHmiSrv.Core.ValueType.Vector });
            if (error != ErrorValue.HMI_SUCCESS)
            {
                command.ExtensionResult = (uint)ExtensionSpecificError.ConfigurationChangeRejected;
                return;
            }
        }

        private DateTime GetStartOfWeek(DateTime d)
        {
            var sunday = d.AddDays(-(double)d.DayOfWeek);
            string weekStartsAt = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, "weekStartsAt");
            return weekStartsAt switch
            {
                "Monday" => sunday.AddDays(1),
                "Tuesday" => sunday.AddDays(2),
                "Wednesday" => sunday.AddDays(3),
                "Thursday" => sunday.AddDays(4),
                "Friday" => sunday.AddDays(5),
                "Saturday" => sunday.AddDays(6),
                "Sunday" => sunday.AddDays(0),
                _ => throw new TcHmiException(ErrorValue.HMI_E_NOT_IMPLEMENTED),
            };
        }

        private static void NotesToMarkdown(StringBuilder builder, Value note, int indent)
        {
            _ = builder.AppendLine(new string(' ', indent * 2) + "- " + note["text"]);
            if (note.ContainsKey("children"))
            {
                foreach (Value child in note["children"])
                {
                    NotesToMarkdown(builder, child, indent + 1);  // recursion: handle notes within notes
                }
            }
        }

        private string ItemsToMarkdown(Value items)
        {
            // compose a summary of all the items that were marked done since the last report
            var builder = new StringBuilder();
            _ = builder.AppendLine("# Done in the week of " + GetStartOfWeek(DateTime.Now).ToLongDateString());
            for (var i = 0; i < items.Count; ++i)
            {
                _ = builder.AppendLine((i + 1).ToString() + ". " + items[i]["name"]);
                if (items[i].ContainsKey("notes"))
                {
                    foreach (Value note in items[i]["notes"])
                    {
                        NotesToMarkdown(builder, note, 1);
                    }
                }
            }

            return builder.ToString();
        }

        private static string ToDoPath(string project, int index)
        {
            return TcHmiApplication.JoinPath("projects", project, "todo[" + index.ToString() + "]");
        }

        private static string DonePath(string project)
        {
            return TcHmiApplication.JoinPath("projects", project, "done");
        }

        private static string DonePath(string project, int index)
        {
            return DonePath(project) + "[" + index.ToString() + "]";
        }

        private static string ReportsPath(string project)
        {
            return TcHmiApplication.JoinPath("projects", project, "reports");
        }
    }
}
