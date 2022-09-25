# Token Voting

Ride smart contract for token-based voting on the Waves blockchain

# Run tests

## Install docker

Link: https://docs.docker.com/engine/install/

## Install dotnet sdk

Link: https://learn.microsoft.com/en-us/dotnet/core/install/

## Run waves private node in docker

Link: https://github.com/wavesplatform/private-node-docker-image

Example:

```bash
docker run --rm -d --name node -p 6869:6869 wavesplatform/waves-private-node
```

## Execute tests

Execute the following command from the project root:

```bash
dotnet test ./tests --nologo -l "console;verbosity=normal"
```