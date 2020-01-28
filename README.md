# durable-function-demo

Azure Durable function that can be triggered by http or a queue and where the http call can either wait for completion or run async

## Sample call via http

```http
POST http://localhost:7071/api/HttpStart
Content-Type: application/json
{ 
	"id":"4bf8e98c-724f-4f15-b946-bdae90370f0e",
	"async":false
}
```

## Sample async call via http

```http
POST http://localhost:7071/api/HttpStart
Content-Type: application/json
{ 

	"id":"687a0b6b-ad42-47c9-a51b-12377347add6",
	"async":true
}
```


