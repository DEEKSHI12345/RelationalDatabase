using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AZURE_AI.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string UserPrompt { get; set; } = string.Empty;

        public List<List<string>> Data { get; set; }
        public string Summary { get; set; }
        public string Error { get; set; }
        public List<TableColumns> TableColumns { get; set; }
        public List<TableSchema> DatabaseSchema { get; set; }

        [BindProperty]
        public string Structure { get; set; }

        [BindProperty]
        public string HiddenQuery { get; set; }

        public string Query { get; set; }
        public List<CheckedColumn> CheckedColumns { get; set; }

        public void OnGet()
        {
            DatabaseSchema = DataService.GetDatabaseSchema();
            if (!string.IsNullOrEmpty(HiddenQuery))
            {
                Query = HiddenQuery;
            }
        }

        public void OnPost()
        {
            if (!string.IsNullOrEmpty(Structure))
            {
                try
                {
                    CheckedColumns = JsonConvert.DeserializeObject<List<CheckedColumn>>(Structure);
                }
                catch (Exception e)
                {
                    Error = "Failed to deserialize Structure: " + e.Message;
                    return;
                }
            }

            if (!string.IsNullOrEmpty(UserPrompt))
            {
                RunQuery(UserPrompt);
            }
            else if (!string.IsNullOrEmpty(HiddenQuery))
            {
                Query = HiddenQuery;
            }
        }

        public void RunQuery(string prompt)
        {
            string OpenAIEndpoint = "https://magicvilla123.openai.azure.com/";
            string OpenAIKey = "1fcd028d8e744609be7ded7d8e4d63d2";
            string deploymentName = "magicvilla";

            OpenAIClient openAIClient = new(new Uri(OpenAIEndpoint), new AzureKeyCredential(OpenAIKey));
            string systemMessage =
                @"
            your are a helpful, cheerful database assistant. 
            use the following database schema when creating your answers:

            - projects (projectid,projectname,startdate,enddate,budget,projectmanagerid,statusid,createddate,updateddate,createdby,updatedby,isactive)
            - members (memberid,membername,email,contact,createddate,updateddate,createdby,updatedby,password,isactive)
            - comments (commentid,commenterid,postedon,comment,taskid,projectid,reply)
            - projectmembers (projectmemberid,projectid,memberid,isactive)
            - roles (roleid,role)
            - statuses (statusid,status)
            - taskmembers (id,taskid,memberid,isactive)
            - tasks (taskid,taskname,taskdetails,projectid,statusid,createddate,updateddate,createdby,updatedby,isactive)
            - userrefreshtokens (email,refreshtoken,id,createddate,createdby,updateddate,updatedby)
            include column name headers in the query results.

            always provide your answer in the json format below:

            { ""summary"": ""your-summary"", ""query"":  ""your-query"" }

            output only json.
            in the preceding json response, substitute ""your-query"" with microsoft sql server query to retrieve the requested data.
            in the preceding json response, substitute ""your-summary"" with a summary of the query.
            always include all columns in the table.
            if the resulting query is non-executable, replace ""your-query"" with na, but still substitute ""your-query"" with a summary of the query.
            ";

            ChatCompletionsOptions chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages = {
                    new ChatRequestSystemMessage(systemMessage),
                    new ChatRequestUserMessage(prompt)
                },
                DeploymentName = deploymentName
            };

            try
            {
                ChatCompletions chatCompletionsResponse = openAIClient.GetChatCompletions(chatCompletionsOptions);

                var responseContent = chatCompletionsResponse.Choices[0].Message.Content;

                if (string.IsNullOrEmpty(responseContent))
                {
                    Error = "OpenAI response content is empty.";
                    return;
                }

                var response = System.Text.Json.JsonSerializer
                    .Deserialize<AIQuery>(responseContent.Replace("```json", "").Replace("```", ""));

                Summary = response.summary;
                Query = response.query;

                if (CheckedColumns != null && CheckedColumns.Count > 0)
                {
                    Query = CombineQueries(Query, CheckedColumns);
                }

                HiddenQuery = Query; // Store the query in the hidden field

                Data = DataService.GetTable(Query);

                var parsedResult = DataService.ParseQuery(Query);
                TableColumns = parsedResult.Select(pr => new TableColumns
                {
                    Table = pr.Table,
                    Columns = pr.Columns
                }).ToList();
            }
            catch (Exception e)
            {
                Error = e.Message;
            }
        }

        private string CombineQueries(string originalQuery, List<CheckedColumn> checkedColumns)
        {
            // Parse the original query to get the tables and columns
            var tablesColumns = DataService.ParseQuery(originalQuery);

            // Convert to a dictionary for easy merging
            var queryDict = tablesColumns.ToDictionary(tc => tc.Table, tc => tc.Columns.ToHashSet());

            // Add the checked columns to the query
            foreach (var checkedColumn in checkedColumns)
            {
                if (queryDict.ContainsKey(checkedColumn.Table))
                {
                    queryDict[checkedColumn.Table].Add(checkedColumn.Column);
                }
                else
                {
                    queryDict[checkedColumn.Table] = new HashSet<string> { checkedColumn.Column };
                }
            }

            // Construct the combined query
            var combinedQuery = string.Join("; ", queryDict.Select(kvp =>
                $"SELECT {string.Join(", ", kvp.Value)} FROM {kvp.Key}"));
            Console.WriteLine(combinedQuery);
            return combinedQuery;
        }
    }

    public class AIQuery
    {
        public string summary { get; set; }
        public string query { get; set; }
    }

    public class TableColumns
    {
        public string Table { get; set; }
        public List<string> Columns { get; set; }
    }

    public class CheckedColumn
    {
        public string Table { get; set; }
        public string Column { get; set; }
    }
}
//using Azure;
//using Azure.AI.OpenAI;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace AZURE_AI.Pages
//{
//    public class IndexModel : PageModel
//    {
//        [BindProperty]
//        public string UserPrompt { get; set; } = string.Empty;

//        public List<List<string>> Data { get; set; }
//        public string Summary { get; set; }
//        public string Error { get; set; }
//        public List<TableColumns> TableColumns { get; set; }
//        public List<TableSchema> DatabaseSchema { get; set; }

//        [BindProperty]
//        public string Structure { get; set; }

//        [BindProperty]
//        public string HiddenQuery { get; set; }

//        public List<CheckedColumn> CheckedColumns { get; set; }

//        public void OnGet()
//        {
//            DatabaseSchema = DataService.GetDatabaseSchema();
//            if (!string.IsNullOrEmpty(HiddenQuery))
//            {
//                Query = HiddenQuery;
//            }
//        }
//        public void OnPost()
//        {
//            if (!string.IsNullOrEmpty(UserPrompt))
//            {
//                RunQuery(UserPrompt);
//            }
//        }
//        public IActionResult OnPostHandleForm()
//        {
//            if (!string.IsNullOrEmpty(Structure))
//            {
//                try
//                {
//                    CheckedColumns = JsonConvert.DeserializeObject<List<CheckedColumn>>(Structure);
//                }
//                catch (Exception e)
//                {
//                    Error = "Failed to deserialize Structure: " + e.Message;
//                    return Partial("_IndexPartial", this);
//                }
//            }

//            if (!string.IsNullOrEmpty(UserPrompt))
//            {
//                RunQuery(UserPrompt);
//            }
//            else if (!string.IsNullOrEmpty(HiddenQuery))
//            {
//                Query = HiddenQuery;
//            }

//            return Partial("_IndexPartial", this);
//        }

//        public string Query { get; set; }

//        public void RunQuery(string prompt)
//        {
//            string OpenAIEndpoint = "https://magicvilla123.openai.azure.com/";
//            string OpenAIKey = "1fcd028d8e744609be7ded7d8e4d63d2";
//            string deploymentName = "magicvilla";

//            OpenAIClient openAIClient = new(new Uri(OpenAIEndpoint), new AzureKeyCredential(OpenAIKey));
//            string systemMessage =
//                @"
//            your are a helpful, cheerful database assistant. 
//            use the following database schema when creating your answers:

//            - projects (projectid,projectname,startdate,enddate,budget,projectmanagerid,statusid,createddate,updateddate,createdby,updatedby,isactive)
//            - members (memberid,membername,email,contact,createddate,updateddate,createdby,updatedby,password,isactive)
//            - comments (commentid,commenterid,postedon,comment,taskid,projectid,reply)
//            - projectmembers (projectmemberid,projectid,memberid,isactive)
//            - roles (roleid,role)
//            - statuses (statusid,status)
//            - taskmembers (id,taskid,memberid,isactive)
//            - tasks (taskid,taskname,taskdetails,projectid,statusid,createddate,updateddate,createdby,updatedby,isactive)
//            - userrefreshtokens (email,refreshtoken,id,createddate,createdby,updateddate,updatedby)
//            include column name headers in the query results.

//            always provide your answer in the json format below:

//            { ""summary"": ""your-summary"", ""query"":  ""your-query"" }

//            output only json.
//            in the preceding json response, substitute ""your-query"" with microsoft sql server query to retrieve the requested data.
//            in the preceding json response, substitute ""your-summary"" with a summary of the query.
//            always include all columns in the table.
//            if the resulting query is non-executable, replace ""your-query"" with na, but still substitute ""your-query"" with a summary of the query.
//            ";

//            ChatCompletionsOptions chatCompletionsOptions = new ChatCompletionsOptions()
//            {
//                Messages = {
//                    new ChatRequestSystemMessage(systemMessage),
//                    new ChatRequestUserMessage(prompt)
//                },
//                DeploymentName = deploymentName
//            };

//            try
//            {
//                ChatCompletions chatCompletionsResponse = openAIClient.GetChatCompletions(chatCompletionsOptions);

//                var responseContent = chatCompletionsResponse.Choices[0].Message.Content;

//                if (string.IsNullOrEmpty(responseContent))
//                {
//                    Error = "OpenAI response content is empty.";
//                    return;
//                }

//                var response = System.Text.Json.JsonSerializer
//                    .Deserialize<AIQuery>(responseContent.Replace("```json", "").Replace("```", ""));

//                Summary = response.summary;
//                Query = response.query;

//                if (CheckedColumns != null && CheckedColumns.Count > 0)
//                {
//                    Query = CombineQueries(Query, CheckedColumns);
//                }

//                HiddenQuery = Query; // Store the query in the hidden field

//                Data = DataService.GetTable(Query);

//                var parsedResult = DataService.ParseQuery(Query);
//                TableColumns = parsedResult.Select(pr => new TableColumns
//                {
//                    Table = pr.Table,
//                    Columns = pr.Columns
//                }).ToList();
//            }
//            catch (Exception e)
//            {
//                Error = e.Message;
//            }
//        }

//        private string CombineQueries(string originalQuery, List<CheckedColumn> checkedColumns)
//        {
//            // Parse the original query to get the tables and columns
//            var tablesColumns = DataService.ParseQuery(originalQuery);

//            // Convert to a dictionary for easy merging
//            var queryDict = tablesColumns.ToDictionary(tc => tc.Table, tc => tc.Columns.ToHashSet());

//            // Add the checked columns to the query
//            foreach (var checkedColumn in checkedColumns)
//            {
//                if (queryDict.ContainsKey(checkedColumn.Table))
//                {
//                    queryDict[checkedColumn.Table].Add(checkedColumn.Column);
//                }
//                else
//                {
//                    queryDict[checkedColumn.Table] = new HashSet<string> { checkedColumn.Column };
//                }
//            }

//            // Construct the combined query
//            var combinedQuery = string.Join(" UNION ALL ",
//                queryDict.Select(kvp => $"SELECT {string.Join(", ", kvp.Value)} FROM {kvp.Key}"));

//            return combinedQuery;
//        }
//    }
//    public class AIQuery
//    {
//        public string summary { get; set; }
//        public string query { get; set; }
//    }

//    public class TableColumns
//    {
//        public string Table { get; set; }
//        public List<string> Columns { get; set; }
//    }

//    public class CheckedColumn
//    {
//        public string Table { get; set; }
//        public string Column { get; set; }
//    }

//}
