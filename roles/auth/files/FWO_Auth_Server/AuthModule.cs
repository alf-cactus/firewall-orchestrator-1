﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FWO_Auth_Server;
using FWO.Auth.Client;
using FWO.Api;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace FWO_Auth
{
    public class AuthModule
    {
        private readonly HttpListener Listener;
        public Ldap[] connectedLdaps;
        private readonly TokenGenerator TokenGenerator;
        private readonly string privateJWTKey = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQCtxiI6Ef+3OJVF
4vkMyqkc4gmCdcTnIRLus2Zrbvm3qkKY6MuBcfIX4Qfn2vvGr4fECComB+64XMON
xduOWn4olmij97kYS/FURZKhH5ghPP/zdrsNlV79CkFIL3wAtxO+/oYnb7Ujep8S
bxBJ5/On2IZ6f4/z/MRb0LX7QsA6ip16b6zs1KcWC8/d3lOnJ24gjXyMOCeWfThP
q1LLmwgISZ5YWoDFtQ59S8ZCuZJv9bFG2f4+OwtmX+FJlkeDgedG71MOKKuQpIF8
ifOy1FQjItY/U/uoqjvCgybf/jDs7FibYC2wmS+X9Zb+M5/C2jDWxkz7CvACUJHI
1o/ZlLd7AgMBAAECggEBAJ0KQ4ArJ+cSkYP43I080JuzgliNyYX+k7d4FQTd43qh
uVGqf87ZhKkjyhs0APjLRGxZ3I1F+exOmMMUnZgGG6DeXG5hvrpAVzWLMjm97aOM
FtqU3/IknRUcIWb00qFq0cN3DRGymAYaGIt2J0hDACUdPlqR0SvzsBgxg2QwLLw5
bqcH423zEJpllEpBA2WvCYtAdpkvnAIFZvAGHS6tX4kp3VqxvmkClX9cDiHOPrzi
bdNirifD/tfGUiY8y/2Dnt74d2hAaFLfI4tDxDMdPnB0rbXhTS2RGkP2fOyJy/m1
pLNIdnpW5V6iVcDGNU/U1fgodR68BrkUMwEMxwq+S7ECgYEA4G5otlX8BsJ1T0ag
kPJ9t2+4/LVI9fX7iJ1cwiNrjLHhyd6PkpvB/aIzNrgubUQW62N3ASZTFSVjOvlg
dPr3V7Ue7XkpSxt5YNp6Y4t8uFoIj4JQUZ3XEi1rx6FeYZEO3lIRheZ6KRRze5uJ
Atc7PpXymkLoMIO+34eJFlLzIx8CgYEAxjeYTGqBLmPC+iNcKoBeTJ3+L3y5c8mh
+H9ObiBCk8zVgUZ43CdD6Qmzx/4foq5b9L5/NRn4uL8pghSQuNbqyyxcZgdIhuv3
5XLar1K95XeInB8CCwZumzJzRYn+4A3feQ4GQ7Rh0RubZm9q87BIL/tYr1wkErJC
6CIPoz9+3CUCgYATPgQmVfr0zWlncaPEqbXTq3WN3TEzPXLihLN2Rbkr5/h26Wkf
5dDdITII6AO7BJJ+fhmu9I09C+aVINp/TSE12Oac7711nhZrEnBZ5pS77aQ8Qa0H
QmQ1P8W06QYBkYFX2Gt+MoOY0BMSrwQxRSjkNdEGHuRvfGw6GBHN4zDLewKBgCYR
AzSZt5lbG1TCea7H3FRGe0xPXaY48NwyRrOril2sFsyu5gMRn18ft+EOkrDBX3OP
Kgreo/+G5sfOf0SgMZM3P79wYqNWqdLszcah00pAPIIPCmtnntI7TBvstn/86g/r
e5SBDdAEx0FS4G1QS2y7jnqO7XaRuXuvHuWxCgHpAoGARykEL8IyW4JRCYLCVHD2
YqysBd77qWhv+bJ8g4I9/hkXXebs/KjXCpISjPqSg/g6I4Efsq3boejB6pkl1/UP
OB5x6nN1lxmzXZ6WtEY+hpdQmkCBCxL0McTtTEQdWfwSy96bnatheTSlL3D7pYib
D1/dZg1p+wIB01vFZQ8RiQU=
-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEArcYiOhH/tziVReL5DMqp
HOIJgnXE5yES7rNma275t6pCmOjLgXHyF+EH59r7xq+HxAgqJgfuuFzDjcXbjlp+
KJZoo/e5GEvxVEWSoR+YITz/83a7DZVe/QpBSC98ALcTvv6GJ2+1I3qfEm8QSefz
p9iGen+P8/zEW9C1+0LAOoqdem+s7NSnFgvP3d5TpyduII18jDgnln04T6tSy5sI
CEmeWFqAxbUOfUvGQrmSb/WxRtn+PjsLZl/hSZZHg4HnRu9TDiirkKSBfInzstRU
IyLWP1P7qKo7woMm3/4w7OxYm2AtsJkvl/WW/jOfwtow1sZM+wrwAlCRyNaP2ZS3
ewIDAQAB
-----END PUBLIC KEY-----
";
        private readonly int hoursValid = 2;
        private readonly string configFile = "../../../../../etc/fworch.yaml";  // todo: replace with abs path in release?
        private readonly Config config;
        private readonly string privateJWTKeyFile;
        private readonly string AuthServerIp;
        private readonly string AuthServerPort;
        private readonly string ApiUri;

        public AuthModule()
        {
            try // reading config file
            { 
                config = new Config(configFile);
                ApiUri =  config.GetConfigValue("api_uri");
                privateJWTKeyFile = config.GetConfigValue("auth_JWT_key_file");
                AuthServerIp = config.GetConfigValue("auth_hostname");
                AuthServerPort = config.GetConfigValue("auth_server_port");
            }
            catch (Exception)
            {
                Console.WriteLine($"Error while trying to read config from file {configFile}\n");
                System.Environment.Exit(1); // exit with error
            }

            try  // move to database and access via Api?
            {
                // privateJWTKey = new StreamReader(privateJWTKeyFile);
                privateJWTKey = File.ReadAllText(privateJWTKeyFile).TrimEnd();
                Console.WriteLine($"JWT Key read from file is {privateJWTKey.Length} bytes long: {privateJWTKey}");
            }
            catch (Exception e)
            {
                ConsoleColor StandardConsoleColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Out.WriteAsync($"Error while trying to read private key : \n Message \n ### \n {e.Message} \n ### \n StackTrace \n ### \n {e.StackTrace} \n ### \n");
                Console.Out.WriteAsync($"Using fallback key! \n");
                Console.ForegroundColor = StandardConsoleColor;
            }

            // Create Http Listener
            Listener = new HttpListener();

            // Create Token Generator
            bool isPrivateKey = true;
            TokenGenerator = new TokenGenerator(AuthClient.ExtractKeyFromPem(privateJWTKey, isPrivateKey), hoursValid);

            // create JWT for auth-server API (relevant part is the role auth-server) calls and add it to the Api connection header 
            APIConnection ApiConn = new APIConnection(ApiUri);
            ApiConn.ChangeAuthHeader(TokenGenerator.CreateJWT(new User { Name = "auth-server", Password = "" }, new UserData(), new Role[] { new Role("auth-server") }));
            
            // fetch all connectedLdaps via API
            Task<Ldap[]> ldapTask = Task.Run(()=> ApiConn.SendQuery<Ldap>(Queries.LdapConnections));
            ldapTask.Wait();
            //Ldap[] connectedLdaps = ldapTask.Result;
            this.connectedLdaps = ldapTask.Result;

            foreach (Ldap connectedLdap in connectedLdaps)
            {
                Console.WriteLine($"Authmodule::Creator: found ldap connection to server {connectedLdap.Address}:{connectedLdap.Port}");
            }
            // Start Http Listener, todo: move to https
            String AuthServerListenerUri = "http://" + AuthServerIp + ":" + AuthServerPort + "/";
            StartListener(AuthServerListenerUri);
        }

        private void StartListener(string AuthServerListenerUri)
        {
            // Add prefixes to listen to 
            Listener.Prefixes.Add(AuthServerListenerUri + "jwt/");

            // Start listener.
            Listener.Start();
            Console.WriteLine($"Auth Server starting on {AuthServerListenerUri}.");

            // GetContext method block while waiting for a request.
            while (true)
            {
                HttpListenerContext context = Listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpStatusCode status = HttpStatusCode.OK;
                string responseString = "";
                try
                {
                    switch (request.Url.LocalPath)
                    {
                        case "/jwt":
                            responseString = CreateJwt(request);
                            break;

                        default:
                            status = HttpStatusCode.InternalServerError;
                            break;
                    }
                }
                catch (Exception e)
                {
                    status = HttpStatusCode.BadRequest;
                    Console.WriteLine($"Error {e.Message}    Stacktrace  {e.StackTrace}");
                }

                // Get response object.
                HttpListenerResponse response = context.Response;
                // Get response stream from response object
                Stream output = response.OutputStream;
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);              
                response.StatusCode = (int)status;
                response.ContentLength64 = buffer.Length;  
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
        }

        private string CreateJwt(User User)
        {
            string responseString = "";

            if (User.Name == "")
            {
                Console.WriteLine("Logging in with anonymous user...");
                responseString = TokenGenerator.CreateJWT(User, null, new Role[] { new Role("anonymous") });
            }                    
            else
            {
                Console.WriteLine($"Try to validate as {User.Name}...");

                // first look for the (first) ldap server with role information
                Ldap roleLdap = null;
                foreach (Ldap connLdap in connectedLdaps) 
                {
                    if (connLdap.RoleSearchPath != "")
                    {
                        roleLdap = connLdap;
                        break;
                    }
                }

                if (roleLdap == null)
                {
                    // TODO: go ahead with anonymous or throw exception?
                    Console.WriteLine("No roles can be determined. Logging in with anonymous user...");
                    responseString = TokenGenerator.CreateJWT(User, null, new Role[] { new Role("anonymous") });
                }
                else
                {
                    // try all configured ldap servers for authentication:
                    responseString = "InvalidCredentials";
                    foreach (Ldap connLdap in connectedLdaps) 
                    {
                        Console.WriteLine($"CreateJwt - trying to authenticate {User} against LDAP {connLdap.Address}:{connLdap.Port} ...");
                        connLdap.Connect();
                        String UserDN = connLdap.ValidateUser(User);
                        if (UserDN!="") 
                        {   
                            // user was successfully authenticated via LDAP
                            Console.WriteLine($"Successfully validated as {User} with DN {UserDN}");
                            // User.UserDN = UserDN;

                            Tenant tenant = new Tenant();
                            tenant.Name = UserDN; //only part of it (first ou)

                            // need to make APICalls available as common library

                            // need to resolve tenant_name from DN to tenant_id first 
                            // query get_tenant_id($tenant_name: String) { tenant(where: {tenant_name: {_eq: $tenant_name}}) { tenant_id } }
                            // variables: {"tenant_name": "forti"}
                            tenant.Id = 0; // todo: replace with APICall() result

                            // get visible devices with the following queries:

                            // query get_visible_mgm_per_tenant($tenant_id:Int!){  get_visible_managements_per_tenant(args: {arg_1: $tenant_id})  id } }
                            String variables = $"\"tenant_id\":{tenant.Id}";
                            // tenant.VisibleDevices = APICall(query,variables);

                            // query get_visible_devices_per_tenant($tenant_id:Int!){ get_visible_devices_per_tenant(args: {arg_1: $tenant_id}) { id }}
                            // variables: {"tenant_id":3}
                            // tenant.VisibleDevices = APICall();
                
                            // tenantInformation.VisibleDevices = {};
                            // tenantInformation.VisibleManagements = [];

                            UserData userData = new UserData();
                            userData.tenant = tenant;
                            responseString = TokenGenerator.CreateJWT(User, userData, roleLdap.GetRoles(UserDN));
                            break;
                        }
                    }
                }
            }  
            return responseString;
        }

        private string CreateJwt(HttpListenerRequest request)
        {
            if (request.HttpMethod == HttpMethod.Post.Method)
            {
                string ParametersJson = new StreamReader(request.InputStream).ReadToEnd();
                Dictionary<string, string> Parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(ParametersJson);
                User User = new User { Name = Parameters["Username"], Password = Parameters["Password"] };
                return CreateJwt(User);
            }
            return $"invalid http method {request.HttpMethod} <> POST";
        }
    }
}
