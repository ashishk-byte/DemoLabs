# Azure Function (Queue Trigger)

Queue Storage Connection: **AzureWebJobsStorage**

Queue Name: **image-queue**

----

Since the Project Template is still **.NET 8**:

**UPDATE** Nuget Packages: 
- SixLabors.ImageSharp (latest version)
- Microsoft.Azure.Functions.Worker" Version="1.24.0" />
- Microsoft.Azure.Functions.Worker.Sdk" Version="1.18.1" />

And, **ADD** Nuget Packages: 
- Azure.Storage.Blobs (latest version)
- SixLabors.ImageSharp (latest version)

Add Project Reference:
- ClassLibrary1

----

Configure the `local.settings.json` file

```
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "BlobContainerName": "student-images",
        "ProcessedBlobContainerName": "processed-images"
    }
}
```
----

Configure the `host.json` file

```
{
    "version": "2.0",
    "extensions": {
        "queues": {
            "maxDequeueCount": 3,
            "visibilityTimeout": "00:00:05"
        }
    },
    "logging": {
        "applicationInsights": {
            "samplingSettings": {
                "isEnabled": true,
                "excludedTypes": "Request"
            },
            "enableLiveMetricsFilters": true
        }
    }
}
```

| Setting                  | Behavior                   |
| ------------------------ | -------------------------- |
| `maxDequeueCount = 3`    | Retry 3 times              |
| `visibilityTimeout = 5s` | Wait 5 sec between retries |

