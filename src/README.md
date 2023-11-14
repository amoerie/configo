# ![alt text](configo.jpg "Title")

# Configo
_________

Configo is a centralised configuration microservice, 
allowing you manage the configuration of a cluster of deployed applications from a single spot.

While other alternatives already exist in this space, none of them have the deep .NET integration Configo offers.

More documentation will appear here later.

# Configo.JsonSchemaGenerator
_____________________________

Use this to automatically generate an appsettings.schema.json file on startup (only in Development)

This schema can be used to simply validate your appsettings.json, but can also be used with Configo to provide intellisense while editing configuration variables.

Usage:

```csharp

services.AddConfigoJsonSchemaGenerator();

```

# Configo.Microsoft.Extensions.Configuration
____________________________________________

Use this to retrieve your application configuration from Configo

Usage:

```

var url = "https://localhost:5000";
var apiKey = "ABC...";
var configuration = new ConfigurationBuilder()
            .AddConfigo(url, apiKey)
            .Build();
```
