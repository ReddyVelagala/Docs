using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;
using Microsoft.PowerBI.Api; 
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Microsoft.PowerBI.Api.Models;
using Microsoft.PowerBI.Api.Models.Credentials;
using System.Data.SqlClient;
using Azure.Identity;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using TenantManagement.Services;

namespace MySampleAzure
{
    public static class CreateWorkSpace
    {
        [FunctionName("CreateWorkSpace")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]  HttpRequest req,
            ILogger log) 
        {
            
            string wsName = req.Query["wsName"];
            string clientId = req.Query["clientId"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            wsName = wsName ?? data?.wsName;
            clientId = clientId ?? data?.clientId;

            string responseMessage = string.IsNullOrEmpty(wsName)
                ? "Pass a workspace name (wsName) in the query string to create a new workspace.\n"
                : $"Workspace with name = {wsName} is getting created.\n";

            responseMessage = string.IsNullOrEmpty(clientId)
                ? responseMessage + "Pass a Client Id (clientId) in the query string to create a new workspace.\n"
                : responseMessage + $"Client Id  = {clientId} is.\n";
             
            if(string.IsNullOrEmpty(wsName)){
                log.LogInformation("No workspace name is given to create.\n");   
                return new OkObjectResult(responseMessage);
            }
            if(string.IsNullOrEmpty(clientId)){
                log.LogInformation("No client Id is given.\n");   
                return new OkObjectResult(responseMessage);
            }

            log.LogInformation($"CreateWorkSpace is called with new worspace name = {wsName}.\n");

            PowerBiServiceApi objservice = new PowerBiServiceApi();

            PowerBiAppIdentity objAppIdentity = objservice.GetAppIdentity();

           try 
           {
                PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);

                var ClientWSes = await objservice. GetTenantWorkspacesAsync(PbiClientDetails);
                
                foreach (Microsoft.PowerBI.Api.Models.Group ws in ClientWSes) {
                    //Console.WriteLine($"ws name = {ws.Name}");
                    if(!string.IsNullOrEmpty(ws.Name)){
                        if (ws.Name.ToLower().Equals(wsName.ToLower())) {
                            log.LogInformation($"CreateWorkSpace - Workspace with name = {wsName} is already existing. Existing Workspace Id = {ws.Id}.\n");  
                            responseMessage = responseMessage + $"The Workspace is already created with this name, available workspace guid is : {ws.Id}.\n";
                            objservice.log(responseMessage, "CreateWorkSpaceAF");
                            return new OkObjectResult(responseMessage);
                        }
                    }
                }
                
                Microsoft.PowerBI.Api.Models.Group workSpace =null;
                string workspaceName = wsName;
                //PowerBiTenantDetails tenantDetails = new PowerBiTenantDetails(); 
            
               workSpace = objservice.CreateAndGetNewWorkSpace(objAppIdentity,workspaceName);
               PbiClientDetails.Groups.AddGroupUser(workSpace.Id, new GroupUser {
                                                                            EmailAddress = "xxx@email.com",
                                                                            GroupUserAccessRight = "Admin"
                                                                        });
                
               log.LogInformation($"The Newly created workspace guid is : {workSpace.Id}.\n");
               responseMessage = responseMessage + $"The Newly created workspace guid is : {workSpace.Id}.\n";
               objservice.log(responseMessage, "CreateWorkSpaceAF");
               objservice.workspaceDbHandle(int.Parse(clientId), workSpace.Id.ToString(), "", workSpace.Name, 1);
            }catch(Exception ex){
               responseMessage = responseMessage + $"Error in creating new workspace. Error messsage is = {ex.Message}";
               objservice.log(responseMessage, "CreateWorkSpaceAF");
               return new OkObjectResult(responseMessage);
            }
             
             return new OkObjectResult(responseMessage);
        } 
        
    }
/*
    public static class CreateAppWorkSpaceAF
    {
        [FunctionName("CreateAppWorkSpaceAF")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]  HttpRequest req,
            ILogger log) 
        {
            log.LogInformation("CreateAppWorkSpaceAF is called\n.");
           // call logging into database here 
            
            string baseWsName = req.Query["baseWsName"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            baseWsName = baseWsName ?? data?.baseWsName;

            string responseMessage = string.IsNullOrEmpty(baseWsName)
                ? "Pass a workspace name (baseWsName) from which a new App is to create, in the query string to create a new workspace.\n"
                : $"Base Workspace with name = {baseWsName} is passed.\n";
             
            string appWsName = req.Query["appWsName"];

            requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            data = JsonConvert.DeserializeObject(requestBody);
            appWsName = appWsName ?? data?.appWsName;

            responseMessage = string.IsNullOrEmpty(appWsName)
                ? "Pass a Appliation workspace name (appWsName) in the query string to create a new workspace.\n"
                : $"App Workspace with name = {appWsName} is getting created.\n";
            
            string appWsDesc = req.Query["appWsDesc"];

            requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            data = JsonConvert.DeserializeObject(requestBody);
            appWsDesc = appWsDesc ?? data?.appWsDesc;

            responseMessage = string.IsNullOrEmpty(appWsDesc)
                ? "Pass a Appliation workspace description (appWsDesc) in the query string to create a new workspace.\n"
                : $"App Workspace with name = {appWsDesc} is passed.\n";

            if(string.IsNullOrEmpty(baseWsName)){
               return  new OkObjectResult(responseMessage);
            }

            if(string.IsNullOrEmpty(appWsName)){
               return  new OkObjectResult(responseMessage);
            }
            
            PowerBiAppIdentity objAppIdentity = new PowerBiAppIdentity();

            objAppIdentity.TenantId ="xxxxxxxx";
            objAppIdentity.ApplicationId ="xxxxxx";
            objAppIdentity.ClientSecret ="xxxxxx";

             PowerBiServiceApi objservice = new PowerBiServiceApi();

             PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);

             var ClientWSes = await objservice.GetTenantWorkspacesAsync(PbiClientDetails);
             
             Group baseWSData = new Group();

             foreach (Group ws in ClientWSes) {
                     //Console.WriteLine($"ws name = {ws.Name}");
                     if(!string.IsNullOrEmpty(ws.Name)){
                        if (ws.Name.ToLower().Equals(baseWsName.ToLower())) {
                          responseMessage = responseMessage + $"The Workspace is found with guid is : {ws.Id}\n";
                          baseWSData = ws;
                          break;
                       }
                     }
             }
              
             //Group workSpace =null;
             //string workspaceName = wsName;
             //PowerBiTenantDetails tenantDetails = new PowerBiTenantDetails();
   
             workSpace = objservice.CreateAndGetAppWorkSpace(objAppIdentity, baseWSData, appWsName, appWsDesc);
             responseMessage = responseMessage + $"The Newly created appworkspace guid is : {workSpace.Id}";
             objservice.log(responseMessage, "CreateWorkSpaceAF");
             return new OkObjectResult(responseMessage);
        } 
        
    }
*/
    public static class DeleteWorkSpace
    {
        [FunctionName("DeleteWorkSpace")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
                        
            string wsName = req.Query["wsName"];
            string clientId = req.Query["clientId"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            wsName = wsName ?? data?.wsName;
            clientId = clientId ?? data?.clientId;

            string responseMessage = string.IsNullOrEmpty(wsName)
                ? "Pass a workspace name (wsName) in the query string to create a new workspace.\n"
                : $"Workspace with name = {wsName} is getting deleted.\n";
            
            responseMessage = string.IsNullOrEmpty(wsName)
                ? responseMessage + "Pass a Client Id (clientId) in the query string to create a new workspace.\n"
                : responseMessage + $"Client Id  = {clientId} is given.\n";

            if(string.IsNullOrEmpty(wsName)){
               log.LogInformation("No workspace name is given to delete.\n");
               return new OkObjectResult(responseMessage);
            }
            if(string.IsNullOrEmpty(clientId)){
                log.LogInformation("No clientId passed.\n");   
                return new OkObjectResult(responseMessage);
            }
            log.LogInformation($"DeleteWorkSpace is called to delete workspace name  = {wsName}.\n");

            PowerBiServiceApi objservice = new PowerBiServiceApi();
            PowerBiAppIdentity objAppIdentity = objservice.GetAppIdentity();

            try
            { 
                PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);

                var ClientWSes = await objservice. GetTenantWorkspacesAsync(PbiClientDetails);

                foreach (Microsoft.PowerBI.Api.Models.Group ws in ClientWSes) {
                    //Console.WriteLine($"ws name = {ws.Name}");
                    if(!string.IsNullOrEmpty(ws.Name)){
                        if (ws.Name.ToLower().Equals(wsName.ToLower())) {

                            objservice.DeleteWorkspaceByClient(PbiClientDetails, ws.Id);
                            objservice.workspaceDbHandle(int.Parse(clientId), ws.Id.ToString(), "", ws.Name, 2);
                            log.LogInformation($"Workspace = {wsName} is deleted successfully.\n");
                            responseMessage = responseMessage + $"Workspace {ws.Name} is deleted successfully.\n";
                            objservice.log(responseMessage, "DeleteeWorkSpaceAF");
                            return new OkObjectResult(responseMessage);
                        }
                    }
                }
                log.LogInformation($"No Workspace = {wsName} is Found.\n");
                responseMessage = responseMessage + $"No Workspace {wsName} is found.\n";
                objservice.log(responseMessage, "DeleteeWorkSpaceAF");
                

            }catch(Exception ex) {
                responseMessage = responseMessage + $"Error in delteting workspace = {wsName}.Error = {ex.Message}.\n";
                objservice.log(responseMessage, "DeleteeWorkSpaceAF");  
                objservice.log(responseMessage, "DeleteeWorkSpaceAF");     
                return new OkObjectResult(responseMessage);
            }
            return new OkObjectResult(responseMessage); 
        
        }
    }
    public static class RemoveUserFromWorkspace
    {
        [FunctionName("RemoveUserFromWorkspace")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            
            string wsName = req.Query["wsName"];
            string userName = req.Query["userName"];   
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            wsName = wsName ?? data?.wsName;
            userName = userName ?? data?.userName;

            string responseMessage = "";

            responseMessage = string.IsNullOrEmpty(wsName)
                ? responseMessage + "Pass a workspace name (wsName) in the query string to delete user from a specified workpsapce.\n"
                : responseMessage + $"User in Workspace = {wsName} is getting deleted.\n";

            responseMessage = string.IsNullOrEmpty(userName)
                ? responseMessage + "Pass a user name (userName) in the query string to delete the specific user from a specified workpsapce.\n"
                : responseMessage + $"User with name = {userName} is getting deleted.\n";
            
            if(string.IsNullOrEmpty(wsName)){
                log.LogInformation($"No workspace name is given.\n");
                return new OkObjectResult(responseMessage);     
            }
             if(string.IsNullOrEmpty(userName)){
                 log.LogInformation($"No user name is given.\n");
                 return new OkObjectResult(responseMessage);     
            }

            log.LogInformation($"RemoveUserFromWorkspace is called to delete {userName} from {wsName}.\n");
            
            PowerBiServiceApi objservice = new PowerBiServiceApi();
            PowerBiAppIdentity objAppIdentity = objservice.GetAppIdentity();

            try
            {
                PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);
                var ClientWSes = await objservice. GetTenantWorkspacesAsync(PbiClientDetails);
                
                foreach (Microsoft.PowerBI.Api.Models.Group ws in ClientWSes) {
                        //Console.WriteLine($"ws name = {ws.Name}");
                        if (ws.Name.Equals(wsName)) {
                            IList<GroupUser> wsUsers = PbiClientDetails.Groups.GetGroupUsers(ws.Id).Value;
                            foreach(GroupUser wsUser in wsUsers){
                                if(!string.IsNullOrEmpty(wsUser.EmailAddress)){
                                        if(wsUser.EmailAddress.ToLower().Equals(userName.ToLower())){
                                            PbiClientDetails.Groups.DeleteUserInGroup(ws.Id, userName);
                                            responseMessage = responseMessage + $"Deleted user = {userName} from workspace = {ws.Name} successfully.\n";
                                            return new OkObjectResult(responseMessage);
                                        }
                                }
                            }
                            
                            responseMessage = responseMessage + $"No user = {userName} found in Workspace {ws.Name}.\n";
                            return new OkObjectResult(responseMessage);
                        }
                }
                responseMessage = responseMessage + $"No workspace = {wsName} found.\n";
                objservice.log(responseMessage, "RemoveUserFromWorkspace");
                log.LogInformation($"{wsName} is not found.\n");

            }catch(Exception ex){
                 responseMessage = responseMessage + $"Error in deleting {userName} from {wsName}.Error = {ex.Message}.\n";  
                 log.LogInformation($"Error in deleting {userName} from {wsName}.Error = {ex.Message}\n");
                 return new OkObjectResult(responseMessage);  
            } 
                  
             return new OkObjectResult(responseMessage);
      }
   }

   public static class AddUserToWorkspace
    {
        [FunctionName("AddUserToWorkspace")]        
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            
            
            string wsName = req.Query["wsName"];
            string userName = req.Query["userName"]; 
            string userRole = req.Query["userRole"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            wsName = wsName ?? data?.wsName;
            userName = userName ?? data?.userName;
            userRole = userRole ?? data?.userRole;

            string responseMessage = "";

            responseMessage = string.IsNullOrEmpty(wsName)
                ? responseMessage + "Pass a Source Workspace Name (wsName) in the query string or in the request body for a personalized response.\n"
                : responseMessage + $"Received Source Workspace Id = , {wsName}.\n";

            responseMessage = string.IsNullOrEmpty(userName)
                ? responseMessage + "Pass a User Name(userName) in the query string for which we want to provide access.\n"
                : responseMessage + $"Received Source User Name = , {userName}.\n";

            responseMessage = string.IsNullOrEmpty(userRole)
                ? responseMessage + "Pass a User Role(userRole) in the query string for which we want to provide access.\n"
                : responseMessage + $"Received Source User Role = , {userRole}.\n";

            if(string.IsNullOrEmpty(wsName)) {
                 log.LogInformation($"No workspace name is given.\n");
                 return new OkObjectResult(responseMessage);
            }
            if(string.IsNullOrEmpty(userName)) {
                 log.LogInformation($"No username is given.\n");
                 return new OkObjectResult(responseMessage);
            }
            if(string.IsNullOrEmpty(userRole)) {
                 log.LogInformation($"No userrole is given.\n");
                 return new OkObjectResult(responseMessage);
            }

            log.LogInformation($"AddUserToWorkspace is called to add {userName} with role {userRole} to workspace {wsName}.");
            
            List<string> Roles = new List<string>();
            Roles.Add("Admin");
            Roles.Add("Member");
            Roles.Add("Viewer");
            Roles.Add("Contributor");
            Roles = Roles.ConvertAll(d => d.ToLower());

            if(!Roles.Contains(userRole.ToLower(), StringComparer.OrdinalIgnoreCase)){
                log.LogInformation($"No role {userRole} is allowed.\n");
                responseMessage = responseMessage + $"No user role {userRole} allowed. User Role can be any of \"Admin\", \"Viewer\", \"Contributor\", \"Member\" only. \n";
                return  new OkObjectResult(responseMessage);
            } 

            PowerBiTenantDetails tenantDetails = new PowerBiTenantDetails();
            
            PowerBiServiceApi objservice = new PowerBiServiceApi();

            PowerBiAppIdentity objAppIdentity = objservice.GetAppIdentity();

            try
            { 
                PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);
                var ClientWSes = await objservice.GetTenantWorkspacesAsync(PbiClientDetails);
                
                Microsoft.PowerBI.Api.Models.Group WSDetails = null;
            
                foreach (Microsoft.PowerBI.Api.Models.Group ws in ClientWSes) {
                    if (!string.IsNullOrEmpty(ws.Name) && ws.Name.ToLower().Equals(wsName.ToLower())) {
                        WSDetails = ws; 
                    }
                }  

                if(WSDetails == null){
                    log.LogInformation($"No worspace {wsName} is found.\n");
                    responseMessage = responseMessage + $"Workspace with workspace name = {wsName} not found.\n";
                    return new OkObjectResult(responseMessage);

                }

                IList<GroupUser> wsUsers = PbiClientDetails.Groups.GetGroupUsers(WSDetails.Id).Value;
                foreach(GroupUser wsUser in wsUsers){
                    if(!string.IsNullOrEmpty(wsUser.EmailAddress)){
                            if(wsUser.EmailAddress.ToLower().Equals(userName.ToLower())){
                                log.LogInformation($"user with {userName} is already present.\n");
                                responseMessage = responseMessage + $"user with {userName} is already present.\n";
                                PbiClientDetails.Groups.DeleteUserInGroup(WSDetails.Id, userName);
                                log.LogInformation($"Deleted {userName} from workspace = {WSDetails.Name}.\n");
                                responseMessage = responseMessage + $"Deleted user = {userName} from workspace = {WSDetails.Name} successfully.\n";
                            }
                    }
                }

                log.LogInformation($"Adding {userName} to workspace = {wsName}.\n");
                responseMessage = responseMessage + $"Adding user = {userName} to workspace = {wsName}.\n";

                PbiClientDetails.Groups.AddGroupUser(WSDetails.Id, new GroupUser {
                                                                            EmailAddress = userName,
                                                                            GroupUserAccessRight = userRole
                                                                        }
                                            );

                log.LogInformation($"Added {userName} to workspace = {wsName} successfully.\n");
                responseMessage = responseMessage + $"Adding user = {userName} to workspace = {wsName} successfully.\n";
                objservice.log(responseMessage, "AddWorkSpaceUserAF");                        
            }catch(Exception ex){
                log.LogInformation($"Error in adding {userName} to workspace = {wsName}.Error = {ex.Message}\n");
                responseMessage = responseMessage + $"Error in Adding user = {userName} to workspace = {wsName}. Error = {ex.Message}\n";
                objservice.log(responseMessage, "AddUserToWorkspace");
                return new OkObjectResult(responseMessage);     
            }
                                         
            return new OkObjectResult(responseMessage);
        }
   }
   public static class CopyAllPowerBIReports
   {
        [FunctionName("CopyAllPowerBIReports")]        
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
        
            string SrcWsName = req.Query["SrcWsName"];
            string DestWsName = req.Query["DestWsName"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            SrcWsName = SrcWsName ?? data?.SrcWsName;
            DestWsName = DestWsName ?? data?.DestWsName;

            string responseMessage = "";

            responseMessage = string.IsNullOrEmpty(SrcWsName)
                ? responseMessage + "Pass a Source Workspace Name (SrcWsName) in the query string. Source Workspace is the workspace name from which reports are to be copied.\n"
                : responseMessage + $"Received Source Workspace Id = s{SrcWsName}.\n";
        
            if(string.IsNullOrEmpty(SrcWsName)){
                log.LogInformation($"No source workspace is given.\n");
                return new OkObjectResult(responseMessage);
            }  

            responseMessage = string.IsNullOrEmpty(DestWsName)
                ? responseMessage + "Pass a Destination Workspace Name (DestWsName) in the query string. Destination Workspace is the workspace name where reports are to be copied.\n"
                : responseMessage + $"Received Destination Workspace Id = {DestWsName}.\n";

            
            if(string.IsNullOrEmpty(DestWsName)){
                log.LogInformation($"No destination workspace is given.\n");
                 return new OkObjectResult(responseMessage);
            }

            log.LogInformation($"CopyAllPowerBIReports is called to copy all reports from source workspace name = {SrcWsName} to destination workspace = {DestWsName}.\n");

            PowerBiTenantDetails srcTenantDetails = new PowerBiTenantDetails();
            PowerBiTenantDetails destTenantDetails = new PowerBiTenantDetails();

            PowerBiServiceApi objservice = new PowerBiServiceApi();
            PowerBiAppIdentity objAppIdentity = objservice.GetAppIdentity();
            try
            {
                PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);
                var ClientWSes = await objservice.GetTenantWorkspacesAsync(PbiClientDetails);
                
                Microsoft.PowerBI.Api.Models.Group SrcWSDetails = null;
                Microsoft.PowerBI.Api.Models.Group DestWsDetails = null;

                foreach (Microsoft.PowerBI.Api.Models.Group ws in ClientWSes) {
                        if (!string.IsNullOrEmpty(ws.Name) && ws.Name.ToLower().Equals(SrcWsName.ToLower())) {
                            SrcWSDetails = ws; 
                        }else if(!string.IsNullOrEmpty(ws.Name) && ws.Name.ToLower().Equals(DestWsName.ToLower())){
                            DestWsDetails = ws;        
                        }
                }  

                if(SrcWSDetails == null){
                    log.LogInformation($"No source workspace {SrcWsName} is found .\n");
                    responseMessage = responseMessage + $"Source Workspace not found with the given Workspace name = {SrcWsName}.\n";
                    return new OkObjectResult(responseMessage);

                }
    
                if(DestWsDetails == null){
                    log.LogInformation($"No destination workspace {DestWsName} is found .\n");
                    responseMessage = responseMessage + $"Destination Workspace not found with the given workspace name  = {DestWsName}.\n";
                    return new OkObjectResult(responseMessage);
                }

                srcTenantDetails.Reports = PbiClientDetails.Reports.GetReportsInGroup(SrcWSDetails.Id).Value;
                destTenantDetails.Reports = PbiClientDetails.Reports.GetReportsInGroup(DestWsDetails.Id).Value;
                string srcDownloadDirectory = Path.Combine(context.FunctionAppDirectory, "Download" );
                System.IO.Directory.CreateDirectory(srcDownloadDirectory);
                //var tempPath = System.IO.Path.GetTempPath();
                responseMessage = responseMessage + $"{srcDownloadDirectory}\n";
                //log.LogInformation($"No destination workspace {DestWsName} is found .\n");
                foreach  (Microsoft.PowerBI.Api.Models.Report srcReport in srcTenantDetails.Reports){
                    
                    bool IsReportDownlaoded = false;
                    IsReportDownlaoded = objservice.GetPowerBiReport(SrcWSDetails, objAppIdentity, srcReport, srcDownloadDirectory); 
                    //Stream inputReportStream = objservice.GetPowerBiReportStream(SrcWSDetails, objAppIdentity, srcReport);
                    if(IsReportDownlaoded) {
                        responseMessage = responseMessage + $"Downloaded Report {srcReport.Name} from source workspace {SrcWsName}.\n";
                        log.LogInformation($"Downloaded Report {srcReport.Name} from source workspace {SrcWsName}.\n");
                        foreach  (Microsoft.PowerBI.Api.Models.Report destReport in destTenantDetails.Reports){
                            if(!string.IsNullOrEmpty(destReport.Name) && destReport.Name.ToLower().Equals(srcReport.Name.ToLower())){
                                responseMessage = responseMessage + $"Report {srcReport.Name} already present in destination workspace {DestWsName}.\n";
                                log.LogInformation($"Report {srcReport.Name} already present in destination workspace {DestWsName}.\n");
                                objservice.DeleteReportFromWorspace(PbiClientDetails,DestWsDetails.Id,destReport.Id );
                                objservice.ReportDbHandle(DestWsDetails.Id.ToString(),destReport.Id.ToString(),destReport.Name, destReport.WebUrl, destReport.EmbedUrl,destReport.DatasetId.ToString(),"",2);
                                responseMessage = responseMessage + $"Deleted Report {srcReport.Name} from destination workspace {DestWsName}.\n";
                                log.LogInformation($"Deleted Report {srcReport.Name} from workspace {DestWsName}.\n");
                            }
                        }
                        
                        string reportPath = srcDownloadDirectory + "/" + srcReport.Name + ".pbix";
                        Microsoft.PowerBI.Api.Models.Report publishedReport = objservice.PublishReportsAndReturnReport(PbiClientDetails, DestWsDetails.Id, reportPath, srcReport.Name);
                        //Microsoft.PowerBI.Api.Models.Report publishedReport = objservice.PublishReportStreamAndReturnReport(PbiClientDetails, DestWsDetails.Id, inputReportStream, srcReport.Name);    
                        Microsoft.PowerBI.Api.Models.Report ReportDetails = new Microsoft.PowerBI.Api.Models.Report();

                        ReportDetails.Name = publishedReport.Name;
                        ReportDetails.Id = publishedReport.Id;
                        //ReportDetails.WebUrl = publishedReport.WebUrl;
                        //ReportDetails.ReportType = publishedReport.ReportType;
                        //ReportDetails.Subscriptions = publishedReport.Subscriptions;
                            
                        destTenantDetails.Reports.Add(ReportDetails);
                        objservice.ReportDbHandle(DestWsDetails.Id.ToString(),publishedReport.Id.ToString(),publishedReport.Name, publishedReport.WebUrl, publishedReport.EmbedUrl,publishedReport.DatasetId.ToString(),"",1);
                        responseMessage = responseMessage + $"Added Report {srcReport.Name} to destination workspace {DestWsName}.\n";
                        log.LogInformation($"Added Report {srcReport.Name} to workspace {DestWsName}.\n");
                    }
                }

            }catch(Exception ex){
                responseMessage = responseMessage + $"Error in copying reports from {SrcWsName} to destination workspace {DestWsName}.Error is = {ex.Message}\n";
                log.LogInformation($"Error in copying reports from {SrcWsName} to destination workspace {DestWsName}.Error is = {ex.Message}.\n");
                objservice.log(responseMessage, "CopyAllPowerBIReports");
                return new OkObjectResult(responseMessage);
            }  
                      
            objservice.log(responseMessage, "CopyAllPowerBIReports");
            return new OkObjectResult(responseMessage);
                      /*
                      int biznexClientId = 10;
                     
                      Dataset dataset = objservice.GetReportDataset(PbiClientDetails, DestWsDetails.Id, srcReport.Name);
                      ClientData clientDbDetails =  objservice.GetBizNexClientData(biznexClientId);
                     UpdateMashupParametersRequest req1 = new UpdateMashupParametersRequest(new List<UpdateMashupParameterDetails>() {
                       new UpdateMashupParameterDetails { Name = "DatabaseServer", NewValue = clientDbDetails.serverName },
                       new UpdateMashupParameterDetails { Name = "DatabaseName", NewValue = clientDbDetails.databaseName }
                     });

                     PbiClientDetails.Datasets.UpdateParametersInGroup(DestWsDetails.Id, dataset.Id, req1);
                     objservice.PatchSqlDatasourceCredentials(PbiClientDetails, DestWsDetails.Id, dataset.Id, clientDbDetails.userName, clientDbDetails.password);
                     PbiClientDetails.Datasets.RefreshDatasetInGroup(DestWsDetails.Id, dataset.Id);
                  */
                

             /*
             string reportsPath =@"C:\DDM\Reports\";
             string[] reportFiles = Directory.GetFiles(reportsPath);
             
             foreach (string filename in reportFiles)    
             {
                reportsPath = filename;
                string importName = filename.Split('/').Last();
                importName = Path.GetFileNameWithoutExtension(importName);
                Guid reportId = objservice.PublishReports(PbiClientDetails, workSpace.Id, reportsPath, importName);

                Report ReportDetails = new Report();

                ReportDetails.Name = importName;
                ReportDetails.Id = reportId;
                ReportDetails.EmbedUrl = filename;
            
                tenantDetails.Reports.Add(ReportDetails);
                
                
            }
        
            //string pbixPath = this.Env.WebRootPath + @"/PBIX/DatasetTemplate.pbix";
            //string importName = "Sales";
            */  

            
        }
    }

   public static class CopyPowerBIReport
   {
        [FunctionName("CopyPowerBIReport")]        
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
        
            string SrcWsName = req.Query["SrcWsName"];
            string DestWsName = req.Query["DestWsName"];
            string ReportName = req.Query["ReportName"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            SrcWsName = SrcWsName ?? data?.SrcWsName;
            DestWsName = DestWsName ?? data?.DestWsName;
            ReportName = ReportName ?? data?.ReportName;

            string responseMessage = "";

            responseMessage = string.IsNullOrEmpty(SrcWsName)
                ? responseMessage + "No Source Workspace is passed. using default workspace (Live) to copy the reports.\n"
                : responseMessage + $"Received Source Workspace Id = s{SrcWsName}.\n";
        
            responseMessage = string.IsNullOrEmpty(DestWsName)
                ? responseMessage + "Pass a Destination Workspace Name (DestWsName) in the query string. Destination Workspace is the workspace name where reports are to be copied.\n"
                : responseMessage + $"Received Destination Workspace Id = {DestWsName}.\n";

            responseMessage = string.IsNullOrEmpty(ReportName)
                ? responseMessage + "Pass a Report Name to copy (ReportName) in the query string.\n"
                : responseMessage + $"Received Report Name to copy = {ReportName}.\n";

            if(string.IsNullOrEmpty(SrcWsName)){
                log.LogInformation($"No source workspace is given.\n");
                return new OkObjectResult(responseMessage);
            }
            if(string.IsNullOrEmpty(DestWsName)){
                log.LogInformation($"No destination workspace is given.\n");
                 return new OkObjectResult(responseMessage);
            }
            if(string.IsNullOrEmpty(ReportName)){
                log.LogInformation($"No Report Name is given.\n");
                 return new OkObjectResult(responseMessage);
            }
            log.LogInformation($"CopyPowerBIReport is called to copy report {ReportName} from source workspace name = {SrcWsName} to destination workspace = {DestWsName}.\n");

            PowerBiTenantDetails srcTenantDetails = new PowerBiTenantDetails();
            PowerBiTenantDetails destTenantDetails = new PowerBiTenantDetails();

            PowerBiServiceApi objservice = new PowerBiServiceApi();
            PowerBiAppIdentity objAppIdentity = objservice.GetAppIdentity();
            try
            {
                PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);
                var ClientWSes = await objservice.GetTenantWorkspacesAsync(PbiClientDetails);
                
                Microsoft.PowerBI.Api.Models.Group SrcWSDetails = null;
                Microsoft.PowerBI.Api.Models.Group DestWsDetails = null;

                foreach (Microsoft.PowerBI.Api.Models.Group ws in ClientWSes) {
                        if (!string.IsNullOrEmpty(ws.Name) && ws.Name.ToLower().Equals(SrcWsName.ToLower())) {
                            SrcWSDetails = ws; 
                        }else if(!string.IsNullOrEmpty(ws.Name) && ws.Name.ToLower().Equals(DestWsName.ToLower())){
                            DestWsDetails = ws;        
                        }
                }  

                if(SrcWSDetails == null){
                    log.LogInformation($"No source workspace {SrcWsName} is found .\n");
                    responseMessage = responseMessage + $"Source Workspace not found with the given Workspace name = {SrcWsName}.\n";
                    return new OkObjectResult(responseMessage);

                }
    
                if(DestWsDetails == null){
                    log.LogInformation($"No destination workspace {DestWsName} is found .\n");
                    responseMessage = responseMessage + $"Destination Workspace not found with the given workspace name  = {DestWsName}.\n";
                    return new OkObjectResult(responseMessage);
                }

                srcTenantDetails.Reports = PbiClientDetails.Reports.GetReportsInGroup(SrcWSDetails.Id).Value;
                destTenantDetails.Reports = PbiClientDetails.Reports.GetReportsInGroup(DestWsDetails.Id).Value;;
                var tempPath = System.IO.Path.GetTempPath();
                responseMessage = responseMessage + $"{tempPath}\n";
                //log.LogInformation($"No destination workspace {DestWsName} is found .\n");
                foreach  (Microsoft.PowerBI.Api.Models.Report srcReport in srcTenantDetails.Reports){

                    if(!string.IsNullOrEmpty(srcReport.Name) && srcReport.Name.ToLower().Equals(ReportName.ToLower())){

                      bool IsReportDownlaoded = false;
                      IsReportDownlaoded = objservice.GetPowerBiReport(SrcWSDetails, objAppIdentity, srcReport, tempPath); 
                      //Stream inputReportStream = objservice.GetPowerBiReportStream(SrcWSDetails, objAppIdentity, srcReport);
                      if(IsReportDownlaoded) {
                          responseMessage = responseMessage + $"Downloaded Report {srcReport.Name} from source workspace {SrcWsName}.\n";
                          log.LogInformation($"Downloaded Report {srcReport.Name} from source workspace {SrcWsName}.\n");
                          foreach  (Microsoft.PowerBI.Api.Models.Report destReport in destTenantDetails.Reports){
                            if(!string.IsNullOrEmpty(destReport.Name) && destReport.Name.ToLower().Equals(srcReport.Name.ToLower())){
                                responseMessage = responseMessage + $"Report {srcReport.Name} already present in destination workspace {DestWsName}.\n";
                                log.LogInformation($"Report {srcReport.Name} already present in destination workspace {DestWsName}.\n");
                                objservice.DeleteReportFromWorspace(PbiClientDetails,DestWsDetails.Id,destReport.Id );
                                objservice.ReportDbHandle(DestWsDetails.Id.ToString(),destReport.Id.ToString(),destReport.Name, destReport.WebUrl, destReport.EmbedUrl,destReport.DatasetId.ToString(),"",2);
                                responseMessage = responseMessage + $"Deleted Report {srcReport.Name} from destination workspace {DestWsName}.\n";
                                log.LogInformation($"Deleted Report {srcReport.Name} from workspace {DestWsName}.\n");
                            }
                          }
                          string reportPath = tempPath + srcReport.Name + ".pbix";
                          Microsoft.PowerBI.Api.Models.Report publishedReport = objservice.PublishReportsAndReturnReport(PbiClientDetails, DestWsDetails.Id, reportPath, srcReport.Name);
                            
                          //Microsoft.PowerBI.Api.Models.Report publishedReport = objservice.PublishReportStreamAndReturnReport(PbiClientDetails, DestWsDetails.Id, inputReportStream, srcReport.Name); 
                            
                          Microsoft.PowerBI.Api.Models.Report ReportDetails = new Microsoft.PowerBI.Api.Models.Report();

                          ReportDetails.Name = publishedReport.Name;
                          ReportDetails.Id = publishedReport.Id;
                          //ReportDetails.WebUrl = publishedReport.WebUrl;
                          //ReportDetails.ReportType = publishedReport.ReportType;
                          //ReportDetails.Subscriptions = publishedReport.Subscriptions;
                            
                          destTenantDetails.Reports.Add(ReportDetails);
                          objservice.ReportDbHandle(DestWsDetails.Id.ToString(),publishedReport.Id.ToString(),publishedReport.Name, publishedReport.WebUrl, publishedReport.EmbedUrl,publishedReport.DatasetId.ToString(),"",1);
                          responseMessage = responseMessage + $"Added Report {srcReport.Name} to destination workspace {DestWsName}.\n";
                          log.LogInformation($"Added Report {srcReport.Name} to workspace {DestWsName}.\n");
                          objservice.log(responseMessage, "CopyPowerBIReport");
                          return new OkObjectResult(responseMessage);
                        }
                    }
                }
                responseMessage = responseMessage + $"No Report with name {ReportName} is fond in source workspace {SrcWsName}.\n";
                log.LogInformation($"No Report with name {ReportName} is fond in source workspace {SrcWsName}.\n");
            }catch(Exception ex){
                responseMessage = responseMessage + $"Error in copying reports from {SrcWsName} to destination workspace {DestWsName}.Error is = {ex.Message}\n";
                log.LogInformation($"Error in copying reports from {SrcWsName} to destination workspace {DestWsName}.Error is = {ex.Message}.\n");
                objservice.log(responseMessage, "CopyPowerBIReport");
                return new OkObjectResult(responseMessage);
            }  
                      
            return new OkObjectResult(responseMessage);
            
        }
    }
    
   public static class DeletePowerBIReport
   {
        [FunctionName("DeletePowerBIReport")]        
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
        
            string SrcWsName = req.Query["wsName"];
            string ReportName = req.Query["ReportName"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            SrcWsName = SrcWsName ?? data?.SrcWsName;
            ReportName = ReportName ?? data?.ReportName;

            string responseMessage = "";

            responseMessage = string.IsNullOrEmpty(SrcWsName)
                ? responseMessage + "No Source Workspace is passed. using default workspace (Live) to copy the reports.\n"
                : responseMessage + $"Received Source Workspace Id = s{SrcWsName}.\n";
        
            responseMessage = string.IsNullOrEmpty(ReportName)
                ? responseMessage + "Pass a Report Name to copy (ReportName) in the query string.\n"
                : responseMessage + $"Received Report Name to copy = {ReportName}.\n";

            if(string.IsNullOrEmpty(SrcWsName)){
                log.LogInformation($"No source workspace is given.\n");
                return new OkObjectResult(responseMessage);
            }
            if(string.IsNullOrEmpty(ReportName)){
                log.LogInformation($"No Report Name is given.\n");
                 return new OkObjectResult(responseMessage);
            }
            log.LogInformation($"DeletePowerBIReport is called to delete report {ReportName} from workspace name = {SrcWsName}.\n");

            PowerBiTenantDetails srcTenantDetails = new PowerBiTenantDetails();
            
            PowerBiServiceApi objservice = new PowerBiServiceApi();
            PowerBiAppIdentity objAppIdentity = objservice.GetAppIdentity();
            try
            {
                PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);
                var ClientWSes = await objservice.GetTenantWorkspacesAsync(PbiClientDetails);
                
                Microsoft.PowerBI.Api.Models.Group SrcWSDetails = null;

                foreach (Microsoft.PowerBI.Api.Models.Group ws in ClientWSes) {
                        if (!string.IsNullOrEmpty(ws.Name) && ws.Name.ToLower().Equals(SrcWsName.ToLower())) {
                            SrcWSDetails = ws; 
                        }
                }  

                if(SrcWSDetails == null){
                    log.LogInformation($"No source workspace {SrcWsName} is found .\n");
                    responseMessage = responseMessage + $"Source Workspace not found with the given Workspace name = {SrcWsName}.\n";
                    return new OkObjectResult(responseMessage);

                }
    
                srcTenantDetails.Reports = PbiClientDetails.Reports.GetReportsInGroup(SrcWSDetails.Id).Value;
                
                var tempPath = System.IO.Path.GetTempPath();
                responseMessage = responseMessage + $"{tempPath}\n";
                //log.LogInformation($"No destination workspace {DestWsName} is found .\n");
                foreach  (Microsoft.PowerBI.Api.Models.Report srcReport in srcTenantDetails.Reports){

                    if(!string.IsNullOrEmpty(srcReport.Name) && srcReport.Name.ToLower().Equals(ReportName.ToLower())){

                              
                                objservice.DeleteReportFromWorspace(PbiClientDetails,SrcWSDetails.Id,srcReport.Id );
                                objservice.ReportDbHandle(SrcWSDetails.Id.ToString(),srcReport.Id.ToString(),srcReport.Name, srcReport.WebUrl, srcReport.EmbedUrl,srcReport.DatasetId.ToString(),"",2);
                                responseMessage = responseMessage + $"Deleted Report {srcReport.Name} from destination workspace {SrcWsName}.\n";
                                log.LogInformation($"Deleted Report {srcReport.Name} from workspace {SrcWsName}.\n");
                          
                                return new OkObjectResult(responseMessage);
                    }
                }
                
                responseMessage = responseMessage + $"No Report with name {ReportName} is fond in source workspace {SrcWsName}.\n";
                log.LogInformation($"No Report with name {ReportName} is fond in source workspace {SrcWsName}.\n");
            }catch(Exception ex){
                responseMessage = responseMessage + $"Error in deleting report {ReportName} from {SrcWsName}.Error is = {ex.Message}\n";
                log.LogInformation($"Error in deleting report {ReportName} from {SrcWsName}..Error is = {ex.Message}.\n");
                objservice.log(responseMessage, "CopyPowerBIReport");
                return new OkObjectResult(responseMessage);
            }  
                      
            return new OkObjectResult(responseMessage);
        }
    }

    public static class DeleteAllPowerBIReports
   {
        [FunctionName("DeleteAllPowerBIReports")]        
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
        
            string SrcWsName = req.Query["wsName"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            SrcWsName = SrcWsName ?? data?.SrcWsName;

            string responseMessage = "";

            responseMessage = string.IsNullOrEmpty(SrcWsName)
                ? responseMessage + "No Source Workspace is passed. using default workspace (Live) to copy the reports.\n"
                : responseMessage + $"Received Source Workspace Id = s{SrcWsName}.\n";

            if(string.IsNullOrEmpty(SrcWsName)){
                log.LogInformation($"No source workspace is given.\n");
                return new OkObjectResult(responseMessage);
            }
            
            log.LogInformation($"DeleteAllPowerBIReports is called to delete reports from workspace name = {SrcWsName}.\n");

            PowerBiTenantDetails srcTenantDetails = new PowerBiTenantDetails();
            
            PowerBiServiceApi objservice = new PowerBiServiceApi();
            PowerBiAppIdentity objAppIdentity = objservice.GetAppIdentity();
            try
            {
                PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);
                var ClientWSes = await objservice.GetTenantWorkspacesAsync(PbiClientDetails);
                
                Microsoft.PowerBI.Api.Models.Group SrcWSDetails = null;

                foreach (Microsoft.PowerBI.Api.Models.Group ws in ClientWSes) {
                        if (!string.IsNullOrEmpty(ws.Name) && ws.Name.ToLower().Equals(SrcWsName.ToLower())) {
                            SrcWSDetails = ws; 
                        }
                }  

                if(SrcWSDetails == null){
                    log.LogInformation($"No source workspace {SrcWsName} is found .\n");
                    responseMessage = responseMessage + $"Source Workspace not found with the given Workspace name = {SrcWsName}.\n";
                    return new OkObjectResult(responseMessage);

                }
    
                srcTenantDetails.Reports = PbiClientDetails.Reports.GetReportsInGroup(SrcWSDetails.Id).Value;
                
                var tempPath = System.IO.Path.GetTempPath();
                responseMessage = responseMessage + $"{tempPath}\n";
                //log.LogInformation($"No destination workspace {DestWsName} is found .\n");
                foreach  (Microsoft.PowerBI.Api.Models.Report srcReport in srcTenantDetails.Reports){
                                objservice.DeleteReportFromWorspace(PbiClientDetails,SrcWSDetails.Id,srcReport.Id );
                                objservice.ReportDbHandle(SrcWSDetails.Id.ToString(),srcReport.Id.ToString(),srcReport.Name, srcReport.WebUrl, srcReport.EmbedUrl,srcReport.DatasetId.ToString(),"",2);
                                responseMessage = responseMessage + $"Deleted Report {srcReport.Name} from destination workspace {SrcWsName}.\n";
                                log.LogInformation($"Deleted Report {srcReport.Name} from workspace {SrcWsName}.\n");
                          
                                return new OkObjectResult(responseMessage);
                    
                }
                
                
            }catch(Exception ex){
                responseMessage = responseMessage + $"Error in deleting reports from {SrcWsName}.Error is = {ex.Message}\n";
                log.LogInformation($"Error in deleting reports from {SrcWsName}..Error is = {ex.Message}.\n");
                objservice.log(responseMessage, "DeleteAllPowerBiReports");
                return new OkObjectResult(responseMessage);
            }  
                      
            return new OkObjectResult(responseMessage);
        }
    } 
   public static class GetEmbeedToken
   {
        [FunctionName("GetEmbeedToken")]        
        public static async Task<String> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
        
            string wsName = req.Query["wsName"];
            string reportName = req.Query["reportName"];
            string Token = "";

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            wsName = wsName ?? data?.wsName;
            reportName = reportName ?? data?.reportName;

            string responseMessage = "";
            /*
            responseMessage = string.IsNullOrEmpty(wsName)
                ? responseMessage + "No Workspace is passed.send {wsName} parameter in query.\n"
                : responseMessage + $"Received Workspace Id = s{wsName}.\n";
        
            responseMessage = string.IsNullOrEmpty(reportName)
                ? responseMessage + "Pass a Report Name (reportName) in the query string. Report Name is the report name for which token to generate.\n"
                : responseMessage + $"Received Report Name = {reportName}.\n";

            */
            if(string.IsNullOrEmpty(wsName)){
                log.LogInformation($"No workspace is passed.\n");
                return responseMessage;
            }
            if(string.IsNullOrEmpty(reportName)){
                log.LogInformation($"No Report Name is passed.\n");
                 return responseMessage;
            }
            
            log.LogInformation($"GetEmbeedToken is called to get token for report {reportName} from workspace name = {wsName}.\n");

            PowerBiTenantDetails TenantDetails = new PowerBiTenantDetails();

            PowerBiServiceApi objservice = new PowerBiServiceApi();
            PowerBiAppIdentity objAppIdentity = objservice.GetAppIdentity();
            try
            {
                PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);
                var ClientWSes = await objservice.GetTenantWorkspacesAsync(PbiClientDetails);
                
                Microsoft.PowerBI.Api.Models.Group WsDetails = null;

                foreach (Microsoft.PowerBI.Api.Models.Group ws in ClientWSes) {
                        if (!string.IsNullOrEmpty(ws.Name) && ws.Name.ToLower().Equals(wsName.ToLower())) {
                            WsDetails = ws; 
                        }
                }  

                if(WsDetails == null){
                    log.LogInformation($"No workspace with name = {wsName} is found .\n");
                    //responseMessage = responseMessage + $"Workspace not found with the given Workspace name = {wsName}.\n";
                    return responseMessage;

                }
                TenantDetails.Reports = PbiClientDetails.Reports.GetReportsInGroup(WsDetails.Id).Value;
                
                foreach  (Microsoft.PowerBI.Api.Models.Report Report in TenantDetails.Reports){
 
                    if(!string.IsNullOrEmpty(Report.Name) && Report.Name.ToLower().Equals(reportName.ToLower())){
                          
                         Token = objservice.GetEmbeddedReportToken(PbiClientDetails, WsDetails.Id, Report.Id);
                         log.LogInformation($"Token created is = {Token}.\n");
                         return Token; 
                    }
                }
                responseMessage = responseMessage + $"No Report with name {reportName} is fond in workspace {wsName}.\n";
                log.LogInformation($"No Report with name {reportName} is fond in source workspace {wsName}.\n");
            }catch(Exception ex){
                //responseMessage = responseMessage + $"Error in copying report {reportName} from {wsName}.Error is = {ex.Message}\n";
                log.LogInformation($"Error in copying report {reportName} from {wsName}.Error is = {ex.Message}.\n");
                objservice.log(responseMessage, "GetReportToken");
                return responseMessage;
            }  
                      
            return responseMessage;
            
        }
    }
    public static class GetEmbeedReport
   {
        [FunctionName("GetEmbeedReport")]        
        public static async Task<String> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
        
            string wsName = req.Query["wsName"];
            string reportName = req.Query["reportName"];
            Microsoft.PowerBI.Api.Models.Report ReportData = null;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            wsName = wsName ?? data?.wsName;
            reportName = reportName ?? data?.reportName;

            string responseMessage = "";
            /*
            responseMessage = string.IsNullOrEmpty(wsName)
                ? responseMessage + "No Workspace is passed.send {wsName} parameter in query.\n"
                : responseMessage + $"Received Workspace Id = s{wsName}.\n";
        
            responseMessage = string.IsNullOrEmpty(reportName)
                ? responseMessage + "Pass a Report Name (reportName) in the query string. Report Name is the report name for which token to generate.\n"
                : responseMessage + $"Received Report Name = {reportName}.\n";

            */
            if(string.IsNullOrEmpty(wsName)){
                log.LogInformation($"No workspace is passed.\n");
                return responseMessage;
            }
            if(string.IsNullOrEmpty(reportName)){
                log.LogInformation($"No Report Name is passed.\n");
                 return responseMessage;
            }
            
            log.LogInformation($"GetEmbeedReport is called to get report for report {reportName} from workspace name = {wsName}.\n");

            PowerBiTenantDetails TenantDetails = new PowerBiTenantDetails();

            PowerBiServiceApi objservice = new PowerBiServiceApi();
            PowerBiAppIdentity objAppIdentity = objservice.GetAppIdentity();
            try
            {
                PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);
                var ClientWSes = await objservice.GetTenantWorkspacesAsync(PbiClientDetails);
                
                Microsoft.PowerBI.Api.Models.Group WsDetails = null;

                foreach (Microsoft.PowerBI.Api.Models.Group ws in ClientWSes) {
                        if (!string.IsNullOrEmpty(ws.Name) && ws.Name.ToLower().Equals(wsName.ToLower())) {
                            WsDetails = ws; 
                        }
                }  

                if(WsDetails == null){
                    log.LogInformation($"No workspace with name = {wsName} is found .\n");
                    //responseMessage = responseMessage + $"Workspace not found with the given Workspace name = {wsName}.\n";
                    return responseMessage;

                }
                TenantDetails.Reports = PbiClientDetails.Reports.GetReportsInGroup(WsDetails.Id).Value;
                
                foreach  (Microsoft.PowerBI.Api.Models.Report Report in TenantDetails.Reports){

                    if(!string.IsNullOrEmpty(Report.Name) && Report.Name.ToLower().Equals(reportName.ToLower())){
                          
                         ReportData = objservice.GetEmbeddedReport(PbiClientDetails, WsDetails.Id, Report.Id);
                         string reportJson = JsonConvert.SerializeObject(ReportData);
                         log.LogInformation($"Report data = {reportJson}.\n");
                         return reportJson; 
                    }
                }
                //responseMessage = responseMessage + $"No Report with name {reportName} is fond in workspace {wsName}.\n";
                log.LogInformation($"No Report with name {reportName} is fond in source workspace {wsName}.\n");
            }catch(Exception ex){
                //responseMessage = responseMessage + $"Error in copying report {reportName} from {wsName}.Error is = {ex.Message}\n";
                log.LogInformation($"Error in copying report {reportName} from {wsName}.Error is = {ex.Message}.\n");
                objservice.log(responseMessage, "GetReportToken");
                return responseMessage;
            }  
                      
            return responseMessage;
            
        }
    }
   public static class DeleteReportFromWorkspace
   {
        [FunctionName("DeleteReportFromWorkspace")]        
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
        
            string SrcWsName = req.Query["wsName"];
            string ReportName = req.Query["reportName"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            SrcWsName = SrcWsName ?? data?.wsName;
            
            ReportName = ReportName ?? data?.reportName;

            string responseMessage = "";

            responseMessage = string.IsNullOrEmpty(SrcWsName)
                ? responseMessage + "No Source Workspace is passed. using default workspace (Live) to copy the reports.\n"
                : responseMessage + $"Received Source Workspace Id = s{SrcWsName}.\n";
        
            responseMessage = string.IsNullOrEmpty(ReportName)
                ? responseMessage + "Pass a Report Name to copy (ReportName) in the query string.\n"
                : responseMessage + $"Received Report Name to copy = {ReportName}.\n";

            if(string.IsNullOrEmpty(SrcWsName)){
                log.LogInformation($"No source workspace is given.\n");
                return new OkObjectResult(responseMessage);
            }
        
            if(string.IsNullOrEmpty(ReportName)){
                log.LogInformation($"No Report Name is given.\n");
                 return new OkObjectResult(responseMessage);
            }
            log.LogInformation($"DeleteReportFromWorkspace is called to delete report {ReportName} from source workspace name = {SrcWsName}.\n");

            PowerBiTenantDetails srcTenantDetails = new PowerBiTenantDetails();

            PowerBiServiceApi objservice = new PowerBiServiceApi();
            PowerBiAppIdentity objAppIdentity = objservice.GetAppIdentity();
            try
            {
                PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);
                var ClientWSes = await objservice.GetTenantWorkspacesAsync(PbiClientDetails);
                
                Microsoft.PowerBI.Api.Models.Group SrcWSDetails = null;

                foreach (Microsoft.PowerBI.Api.Models.Group ws in ClientWSes) {
                        if (!string.IsNullOrEmpty(ws.Name) && ws.Name.ToLower().Equals(SrcWsName.ToLower())) {
                            SrcWSDetails = ws; 
                        }
                }  

                if(SrcWSDetails == null){
                    log.LogInformation($"No workspace {SrcWsName} is found .\n");
                    responseMessage = responseMessage + $"Workspace not found with the given Workspace name = {SrcWsName}.\n";
                    return new OkObjectResult(responseMessage);

                }
    
                srcTenantDetails.Reports = PbiClientDetails.Reports.GetReportsInGroup(SrcWSDetails.Id).Value;

                //log.LogInformation($"No destination workspace {DestWsName} is found .\n");
                foreach  (Microsoft.PowerBI.Api.Models.Report Report in srcTenantDetails.Reports){

                    if(!string.IsNullOrEmpty(Report.Name) && Report.Name.ToLower().Equals(ReportName.ToLower())){

                     
                     
                                objservice.DeleteReportFromWorspace(PbiClientDetails,SrcWSDetails.Id,Report.Id );
                                responseMessage = responseMessage + $"Deleted Report {ReportName} from workspace {SrcWsName}.\n";
                                log.LogInformation($"Deleted Report {ReportName} from workspace {SrcWsName}.\n");
                            }
                }
               
                responseMessage = responseMessage + $"No Report with name {ReportName} is found in workspace {SrcWsName}.\n";
                log.LogInformation($"No Report with name {ReportName} is found in source workspace {SrcWsName}.\n");
            }catch(Exception ex){
                responseMessage = responseMessage + $"Error in deleting report {ReportName} from {SrcWsName}.Error is = {ex.Message}\n";
                log.LogInformation($"Error in deleting {ReportName} report from {SrcWsName}.Error is = {ex.Message}.\n");
                objservice.log(responseMessage, "DeleteReportFromWorkspace");
                return new OkObjectResult(responseMessage);
            }  
                      
            return new OkObjectResult(responseMessage);
                      /*
                      int biznexClientId = 10;
                     
                      Dataset dataset = objservice.GetReportDataset(PbiClientDetails, DestWsDetails.Id, srcReport.Name);
                      ClientData clientDbDetails =  objservice.GetBizNexClientData(biznexClientId);
                     UpdateMashupParametersRequest req1 = new UpdateMashupParametersRequest(new List<UpdateMashupParameterDetails>() {
                       new UpdateMashupParameterDetails { Name = "DatabaseServer", NewValue = clientDbDetails.serverName },
                       new UpdateMashupParameterDetails { Name = "DatabaseName", NewValue = clientDbDetails.databaseName }
                     });

                     PbiClientDetails.Datasets.UpdateParametersInGroup(DestWsDetails.Id, dataset.Id, req1);
                     objservice.PatchSqlDatasourceCredentials(PbiClientDetails, DestWsDetails.Id, dataset.Id, clientDbDetails.userName, clientDbDetails.password);
                     PbiClientDetails.Datasets.RefreshDatasetInGroup(DestWsDetails.Id, dataset.Id);
                  */
                

             /*
             string reportsPath =@"C:\DDM\Reports\";
             string[] reportFiles = Directory.GetFiles(reportsPath);
             
             foreach (string filename in reportFiles)    
             {
                reportsPath = filename;
                string importName = filename.Split('/').Last();
                importName = Path.GetFileNameWithoutExtension(importName);
                Guid reportId = objservice.PublishReports(PbiClientDetails, workSpace.Id, reportsPath, importName);

                Report ReportDetails = new Report();

                ReportDetails.Name = importName;
                ReportDetails.Id = reportId;
                ReportDetails.EmbedUrl = filename;
            
                tenantDetails.Reports.Add(ReportDetails);
                
                
            }
        
            //string pbixPath = this.Env.WebRootPath + @"/PBIX/DatasetTemplate.pbix";
            //string importName = "Sales";
            */  

            
        }
    } 

    public static class AddAADGroupToWorkspace
    {
        [FunctionName("AddAADGroupToWorkspace")]        
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            
            string wsName = req.Query["wsName"];
            string userGroupName = req.Query["userGroupName"]; 
            string userGroupRole = req.Query["userGroupRole"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            wsName = wsName ?? data?.wsName;
            userGroupName = userGroupName ?? data?.userGroupName;
            userGroupRole = userGroupRole ?? data?.userGroupRole;

            string responseMessage = ""; 

            responseMessage = string.IsNullOrEmpty(wsName)
                ? responseMessage + "Pass a Source Workspace Name (wsName) in the query string or in the request body for a personalized response.\n"
                : responseMessage + $"Received Source Workspace Id = , {wsName}.\n";

            responseMessage = string.IsNullOrEmpty(userGroupName)
                ? responseMessage + "Pass a User Grop Name (userGroupName) in the query string for which we want to provide access.\n"
                : responseMessage + $"Received Source User Name = , {userGroupName}.\n";
            responseMessage = string.IsNullOrEmpty(userGroupRole)
                ? responseMessage + "Pass a User Role(userRole) in the query string for which we want to provide access.\n"
                : responseMessage + $"Received Source User Role = , {userGroupRole}.\n";

            if(string.IsNullOrEmpty(wsName)){
                log.LogInformation($"No workspace name passed in query.");
                return new OkObjectResult(responseMessage);
            }
            if(string.IsNullOrEmpty(userGroupName)){
                log.LogInformation($"No user group name passed in query.");
                return new OkObjectResult(responseMessage);
            }  
            if(string.IsNullOrEmpty(userGroupRole)){
                log.LogInformation($"No user group role passed in query.");
                return new OkObjectResult(responseMessage);
            }

            log.LogInformation($"AddAADGroupToWorkspace is called to add user group {userGroupName} as role {userGroupRole} to workspace {wsName}.\n");
            
            List<string> Roles = new List<string>();
            Roles.Add("Admin");
            Roles.Add("Member");
            Roles.Add("Viewer");
            Roles.Add("Contributor");
            Roles = Roles.ConvertAll(d => d.ToLower());
            if(!Roles.Contains(userGroupRole.ToLower(), StringComparer.OrdinalIgnoreCase)){
                log.LogInformation($"No user group role {userGroupRole} is allwoed.\n");
                responseMessage = responseMessage + $"User Group role = {userGroupRole} is not allowed. User Role can be any of \"Admin\", \"Viewer\", \"Contributor\", \"Member\" only.\n";
                return  new OkObjectResult(responseMessage);
            } 
            PowerBiTenantDetails tenantDetails = new PowerBiTenantDetails();
            PowerBiServiceApi objservice = new PowerBiServiceApi();
            PowerBiAppIdentity objAppIdentity = objservice.GetAppIdentity();

            try
            {
                PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);
                var ClientWSes = await objservice.GetTenantWorkspacesAsync(PbiClientDetails);
                
                Microsoft.PowerBI.Api.Models.Group WSDetails = null;
            
                foreach (Microsoft.PowerBI.Api.Models.Group ws in ClientWSes) {
                        if (!string.IsNullOrEmpty(ws.Name) && ws.Name.ToLower().Equals(wsName.ToLower())) {
                            WSDetails = ws; 
                        }
                }  

                if(WSDetails == null){
                    log.LogInformation($"No workspace {wsName} is found.\n");
                    responseMessage = responseMessage + $"Workspace with workspace name = {wsName} not found.\n";
                    return new OkObjectResult(responseMessage);
                }

                IList<GroupUser> wsUsers = PbiClientDetails.Groups.GetGroupUsers(WSDetails.Id).Value;
                foreach(GroupUser wsUser in wsUsers){
                    if(!string.IsNullOrEmpty(wsUser.EmailAddress)){
                            if(wsUser.EmailAddress.ToLower().Equals(userGroupName.ToLower())){
                                log.LogInformation($"User Group with name {userGroupName} is already present.\n");
                                responseMessage = responseMessage + $"User Group with name {userGroupName} is already present.\n";
                                PbiClientDetails.Groups.DeleteUserInGroup(WSDetails.Id, userGroupName);
                                log.LogInformation($"Deleted user = {userGroupName} from workspace = {WSDetails.Name} successfully.\n");
                                //responseMessage = responseMessage + $"User Group with name {userGroupName} is already present.\n";
                                responseMessage = responseMessage + $"Deleted user = {userGroupName} from workspace = {WSDetails.Name} successfully.\n";
                                
                            }
                    }
                }
             
                var tenantId = "xxxxxxxx";
                // Values from app registration
                var clientId = "xxxxxxxxx";
                var secret = "xxxxxxx";

                IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                                                                                    .Create(clientId)
                                                                                    .WithTenantId(tenantId)
                                                                                    .WithClientSecret(secret)
                                                                                    .Build();

                IAuthenticationProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);
                GraphServiceClient graphClient = new GraphServiceClient(authProvider);

                var groupsDetails = graphClient.Groups.Request()
                        .Filter($"startswith(displayName,'{userGroupName}')")
                        .GetAsync()
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult()
                        .ToList()
                        .Where(x => string.Equals(x.DisplayName, userGroupName, StringComparison.InvariantCultureIgnoreCase))
                        .FirstOrDefault();

                if(groupsDetails  == null){
                    log.LogInformation($"No User Group with name {userGroupName} is found in active directory search.\n");
                    responseMessage = responseMessage+$"No User Group with name {userGroupName} is found in active directory search.\n";
                    return new OkObjectResult(responseMessage);
                }

                var groupObjectId = groupsDetails.Id;
                
                PbiClientDetails.Groups.AddGroupUser(WSDetails.Id, new GroupUser {
                                                                            //EmailAddress = userGroupName,
                                                                            Identifier = groupObjectId.ToString(),
                                                                            //DisplayName = userGroupName,
                                                                            GroupUserAccessRight = userGroupRole,
                                                                            PrincipalType = "Group"

                                                                        });
                log.LogInformation($"Added User Group with name {userGroupName} as {userGroupRole} to workspace {wsName} successfully.\n");
                responseMessage = responseMessage+$"Added User Group with name {userGroupName} as {userGroupRole} to workspace {wsName} successfully.\n";                                                         
        }catch(Exception ex) {
            log.LogInformation($"Error in Adding User Group with name {userGroupName} as {userGroupRole} to workspace {wsName}.Error is = {ex.Message}\n");
            responseMessage = responseMessage+$"Error in Adding User Group with name {userGroupName} as {userGroupRole} to workspace {wsName}.Error is = {ex.Message}\n";
            objservice.log(responseMessage, "AddWorkSpaceUserGroupAF"); 
            return new OkObjectResult(responseMessage);  
        }                             
        objservice.log(responseMessage, "AddWorkSpaceUserGroupAF");                             
        return new OkObjectResult(responseMessage);    
        } 
    }
    public static class RemoveAADGroupFromWorkspace
    {
        [FunctionName("RemoveAADGroupFromWorkspace")]        
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            
            string wsName = req.Query["wsName"];
            string userGroupName = req.Query["userGroupName"]; 
            string userGroupRole = req.Query["userGroupRole"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            wsName = wsName ?? data?.wsName;
            userGroupName = userGroupName ?? data?.userGroupName;
            userGroupRole = userGroupRole ?? data?.userGroupRole;

            string responseMessage = ""; 

            responseMessage = string.IsNullOrEmpty(wsName)
                ? responseMessage + "Pass a Source Workspace Name (wsName) in the query string or in the request body for a personalized response.\n"
                : responseMessage + $"Received Source Workspace Id = , {wsName}.\n";

            responseMessage = string.IsNullOrEmpty(userGroupName)
                ? responseMessage + "Pass a User Grop Name (userGroupName) in the query string for which we want to provide access.\n"
                : responseMessage + $"Received Source User Name = , {userGroupName}.\n";
            responseMessage = string.IsNullOrEmpty(userGroupRole)
                ? responseMessage + "Pass a User Role(userRole) in the query string for which we want to provide access.\n"
                : responseMessage + $"Received Source User Role = , {userGroupRole}.\n";

            if(string.IsNullOrEmpty(wsName)){
                log.LogInformation($"No workspace name passed in query.");
                return new OkObjectResult(responseMessage);
            }
            if(string.IsNullOrEmpty(userGroupName)){
                log.LogInformation($"No user group name passed in query.");
                return new OkObjectResult(responseMessage);
            }  
            if(string.IsNullOrEmpty(userGroupRole)){
                log.LogInformation($"No user group role passed in query.");
                return new OkObjectResult(responseMessage);
            }

            log.LogInformation($"RemoveAADGroupFromWorkspace is called to remove user group {userGroupName} as role {userGroupRole} from workspace {wsName}.\n");
            
             
            PowerBiTenantDetails tenantDetails = new PowerBiTenantDetails();
            PowerBiServiceApi objservice = new PowerBiServiceApi();
            PowerBiAppIdentity objAppIdentity = objservice.GetAppIdentity();

            try
            {
                PowerBIClient PbiClientDetails = objservice.GetPowerBiClient(objAppIdentity);
                var ClientWSes = await objservice.GetTenantWorkspacesAsync(PbiClientDetails);
                
                Microsoft.PowerBI.Api.Models.Group WSDetails = null;
            
                foreach (Microsoft.PowerBI.Api.Models.Group ws in ClientWSes) {
                        if (!string.IsNullOrEmpty(ws.Name) && ws.Name.ToLower().Equals(wsName.ToLower())) {
                            WSDetails = ws; 
                        }
                }  

                if(WSDetails == null){
                    log.LogInformation($"No workspace {wsName} is found.\n");
                    responseMessage = responseMessage + $"Workspace with workspace name = {wsName} not found.\n";
                    return new OkObjectResult(responseMessage);
                }

                IList<GroupUser> wsUsers = PbiClientDetails.Groups.GetGroupUsers(WSDetails.Id).Value;
                foreach(GroupUser wsUser in wsUsers){
                    if(!string.IsNullOrEmpty(wsUser.EmailAddress)){
                            if(wsUser.EmailAddress.ToLower().Equals(userGroupName.ToLower())){
                               
                                PbiClientDetails.Groups.DeleteUserInGroup(WSDetails.Id, userGroupName);
                                log.LogInformation($"Deleted user = {userGroupName} from workspace = {WSDetails.Name} successfully.\n");
                                //responseMessage = responseMessage + $"User Group with name {userGroupName} is already present.\n";
                                responseMessage = responseMessage + $"Deleted user = {userGroupName} from workspace = {WSDetails.Name} successfully.\n";
                                return new OkObjectResult(responseMessage);
                            }
                    }
                }
                log.LogInformation($"No User Group with name {userGroupName} found in workspace {wsName}.\n");
                responseMessage = responseMessage+$"No User Group with name {userGroupName} found in workspace {wsName}..\n";                                                         
        }catch(Exception ex) {
            log.LogInformation($"Error in Removing User Group with name {userGroupName} as {userGroupRole} to workspace {wsName}.Error is = {ex.Message}\n");
            responseMessage = responseMessage+$"Error in Removing User Group with name {userGroupName} as {userGroupRole} to workspace {wsName}.Error is = {ex.Message}\n";
            objservice.log(responseMessage, "RemoveAADGroupFromWorkspace"); 
            return new OkObjectResult(responseMessage);  
        }                             
        objservice.log(responseMessage, "RemoveAADGroupFromWorkspace");                             
        return new OkObjectResult(responseMessage);    
        } 
    } 
}