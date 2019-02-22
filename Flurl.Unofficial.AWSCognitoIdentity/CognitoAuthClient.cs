using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Auth;
using Flurl.Http;

namespace Flurl.Unofficial.AWSCognitoIdentification {
    /// <summary>
    /// AWS Cognito Identification Authenticator
    /// </summary>
    public abstract class CognitoAuthClient {
        protected abstract string ServiceURL { get; }
        protected abstract string PoolID { get; }
        protected abstract string IdentityPoolID { get; }
        protected abstract string ClientID { get; }
        protected abstract RegionEndpoint RegionEndpoint { get; }
        
        /// <summary>Spoofed user agent string to use</summary>
        public const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";
        
        private AmazonCognitoIdentityProviderClient _cognitoProvider;
        private CognitoUser _user;
        private AuthFlowResponse _authFlowResponse;
        private AWSCredentials _awsCredentials;
        
        private static readonly FakeExecuteAPIConfig _fakeExecuteApiConfig = new FakeExecuteAPIConfig();
        private static readonly AWS4Signer _signer = new AWS4Signer();

        /// <summary>
        /// Sign into Cognito Identity
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        public async Task LoginAsync(string username, string password) {
            var config = new AmazonCognitoIdentityProviderConfig {
                ServiceURL = ServiceURL, RegionEndpoint = RegionEndpoint
            };
            _cognitoProvider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), config);
            var userPool = new CognitoUserPool(PoolID, ClientID, _cognitoProvider);
            _user = new CognitoUser(username, ClientID, userPool, _cognitoProvider);
            AuthFlowResponse authResponse = await _user.StartWithSrpAuthAsync(new InitiateSrpAuthRequest {
                Password = password
            });
            _authFlowResponse = authResponse;
            _awsCredentials = _user.GetCognitoAWSCredentials(IdentityPoolID, RegionEndpoint);
        }
        
        /// <summary>
        /// Lazily authenticate an <see cref="IFlurlRequest"/> with AWS Cognito
        /// </summary>
        /// <param name="request">The request to be authenticated</param>
        /// <returns>The request</returns>
        public IFlurlRequest AuthenticateRequest(IFlurlRequest request) {
            request.Settings.BeforeCallAsync += PrepareCognitoCallAsync;
            return request;
        }
        
        /// <summary>
        /// Authenticate a call that will be handled by AWS
        /// </summary>
        /// <param name="call">The associated <see cref="HttpCall"/></param>
        /// <returns></returns>
        private async Task PrepareCognitoCallAsync(HttpCall call) {
            // this has to be done lazily so we have info about what the call actually is. body, method etc
            var request = call.FlurlRequest;
            string root = new Url(request.Url).ResetToRoot();
            var resourcePath = ReplaceFirst(new Url(request.Url) {Query = null}.ToString(), root, string.Empty);
            
            var credentials = _awsCredentials.GetCredentials();
            
            // create fake request for signing method
            DefaultRequest awsRequest = new DefaultRequest(new FakeAWSRequest(), FakeExecuteAPIConfig.ServiceName) {
                AuthenticationRegion = RegionEndpoint.SystemName,
                
                HttpMethod = call.Request.Method.ToString().ToUpper(),
                Endpoint = new Uri(root),
                ResourcePath = resourcePath
            };
            
            // add content
            if (call.Request.Content != null) {
                awsRequest.Content = await call.Request.Content.ReadAsByteArrayAsync();
            } else {
                awsRequest.Content = new byte[0];
            }
            
            // add query params
            foreach (QueryParameter parameter in request.Url.QueryParams) {
                awsRequest.Parameters[parameter.Name] = parameter.Value.ToString();
            }
            
            _signer.Sign(awsRequest, _fakeExecuteApiConfig, null, credentials.AccessKey, credentials.SecretKey);

            // retrieve headers from fake structure
            request.Headers["Authorization"] = awsRequest.Headers["Authorization"];
            request.Headers["x-amz-date"] = awsRequest.Headers["x-amz-date"];
            request.Headers["x-amz-content-sha256"] = awsRequest.Headers["x-amz-content-sha256"];
            
            request.WithHeader("user-agent", UserAgent)  // spoof agent :)
                   .WithHeader("x-amz-security-token", credentials.Token)
                   .WithHeader("access-key", _authFlowResponse.AuthenticationResult.AccessToken);
        }
        
        public void Dispose() {
            _authFlowResponse = null;
            _user?.SignOut();
            _user = null;
            _cognitoProvider?.Dispose();
        }
        
        // -- HELPERS --
        
        /// <summary>
        /// Replace the first instance of a string in another string
        /// </summary>
        /// <param name="text">Text that will be searched</param>
        /// <param name="search">What will be searched for</param>
        /// <param name="replace">What it will be replaced with</param>
        /// <returns>The string with the first occurence replaced</returns>
        private static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search, StringComparison.Ordinal);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
    }

    internal class FakeAWSRequest : AmazonWebServiceRequest {
        // just needs to be non-null, no actual functionality
        // i'd rather use this class then any existing just to prevent any unintentional behavior
    }
        
    internal class FakeExecuteAPIConfig : ClientConfig {
        public const string ServiceName = "execute-api";

        public override string ServiceVersion => "1970-01-01";
        public override string UserAgent => CognitoAuthClient.UserAgent;
        public override string RegionEndpointServiceName => ServiceName;

        public FakeExecuteAPIConfig() {
            AuthenticationServiceName = ServiceName;
        }
    }
}