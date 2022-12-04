# .Net Lambda APIGateway Runner [Experimental]

- Just Testing



```sh
# Example Only
docker run \
--name lambda \
-e PACKAGEURL=[URL for Download Published Package (http://storage/xxxx.zip)] \
-e HANDLER=[Project1::Project1.LambdaEntryPoint::FunctionHandlerAsync] 
-p 30081:80 \
-d  alexcheung/lambdaapigatewayrunner:20221204


docker run \
--name lambda \
-e PACKAGEURL=http://192.168.20.12:30080/package/00000000-0000-0000-0000-000000000000.zip \
-e HANDLER=Project1::Project1.LambdaEntryPoint::FunctionHandlerAsync \
-p 30081:80 \
-d  alexcheung/lambdaapigatewayrunner:20221204
```

