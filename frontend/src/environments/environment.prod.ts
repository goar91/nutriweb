export const environment = {
  production: true,
  auth0: {
    domain: 'YOUR_AUTH0_DOMAIN.auth0.com',
    clientId: 'YOUR_AUTH0_CLIENT_ID',
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: 'YOUR_AUTH0_API_IDENTIFIER'
    }
  },
  apiUrl: 'https://your-production-api.com/api'
};
