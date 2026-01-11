export const environment = {
  production: false,
  auth0: {
    domain: 'YOUR_AUTH0_DOMAIN.auth0.com',
    clientId: 'YOUR_AUTH0_CLIENT_ID',
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: 'YOUR_AUTH0_API_IDENTIFIER'
    }
  },
  apiUrl: 'http://localhost:5000/api'
};
