using Flurl.Http;

namespace Flurl.Unofficial.AWSCognitoIdentification {
    public static class Extensions {
        /// <summary>
        /// Lazily authenticate an <see cref="IFlurlRequest"/> with AWS Cognito
        /// </summary>
        /// <param name="request">The request to be authenticated</param>
        /// <param name="authClient">Cognito client</param>
        /// <returns>The request</returns>
        public static IFlurlRequest WithCognitoAuth(this IFlurlRequest request, CognitoAuthClient authClient) => authClient.AuthenticateRequest(request);
        
        /// <see cref="Extensions.WithCognitoAuth(IFlurlRequest, CognitoAuthClient)"/>
        public static IFlurlRequest WithCognitoAuth(this Url url, CognitoAuthClient authClient) => new FlurlRequest(url).WithCognitoAuth(authClient);
        
        /// <see cref="Extensions.WithCognitoAuth(IFlurlRequest, CognitoAuthClient)"/>
        public static IFlurlRequest WithCognitoAuth(this string url, CognitoAuthClient authClient) => new Url(url).WithCognitoAuth(authClient);
    }
}