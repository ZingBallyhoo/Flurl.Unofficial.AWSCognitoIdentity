# Flurl.Unofficial.AWSCognitoIdentity

[![NuGet](https://img.shields.io/nuget/v/Flurl.Unofficial.AWSCognitoIdentity.svg?maxAge=86400)](https://www.nuget.org/packages/Flurl.Unofficial.AWSCognitoIdentity/)

A small helper to authenticate [Flurl](https://github.com/tmenier/Flurl) requests for AWS Cognito Identity

## How to login:

1. Create a subclass of `CognitoAuthClient`.
2. Implement the following abstract properties depending on your setup:
   * `ServiceURL`
   * `PoolID`
   * `IdentityPoolID`
   * `ClientID`
   * `RegionEndpoint`
3. Create an instance of your client and call `LoginAsync(string username, string password)` when you are ready to login.

## How to authenticate a request

1. Import `Extensions` and call `WithCognitoAuth(CognitoAuthClient)`, passing in your logged in client as the single parameter. (returns the request for chaining)
2. Dispatch as you normally would, authentication is handled lazily in the background on an event.

### Example:
```csharp
"https://example.com/real_cognito_endpoint/hey".WithCognitoAuth().GetJsonAsync();
```

## Notes:
* Only async
* [Spoofs user agent string](https://github.com/ZingBallyhoo/Flurl.Unofficial.AWSCognitoIdentity/blob/master/Flurl.Unofficial.AWSCognitoIdentity/CognitoAuthClient.cs#L23)

## Dependencies:
* [Flurl 2.8.1](https://www.nuget.org/packages/Flurl/)
* [Flurl.Http 2.4.1](https://www.nuget.org/packages/Flurl.Http/)
* [AWSSDK.CognitoIdentityProvider 3.3.12.16](https://www.nuget.org/packages/AWSSDK.CognitoIdentityProvider/)
* [AWSSDK.Extensions.CognitoAuthentication 0.9.4](https://www.nuget.org/packages/AWSSDK.Extensions.CognitoAuthentication/)
