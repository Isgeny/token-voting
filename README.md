# Token Voting

Ride smart contract for token-based voting on the Waves blockchain.

An contract's owner can set:

- Several items for voting with multiple options
- Asset ID
- Start and end voting height
- Quorum percent

A voter can:

- Put tokens and put more later during voting
- Cast a vote and change it later during voting
- Withdraw tokens back after voting ends

## Interface

- `@Callable(i) func constructor(availableOptions: String, votingAsset: String, startHeight: Int, endHeight: Int, quorumPercent: Int)`
  - Can be invoked by a contract's owner to configure voting. Can be called only once.
  - `availableOptions` -
    a string of available options for voting, which is separated by `,` (comma character).
    Example: voting for increasing or decreasing of parameter "a" can be set as `increase-a,decrease-a`.
    To make voting with multiple items use `:` (colon character).
    Example: voting with two items: 1. increasing or decreasing of parameter "a" 2. increasing, decreasing or keeping of
    parameter "b"
    can be set
    as `decrease-a:decrease-b,decrease-a:increase-b,decrease-a:keep-b,increase-a:decrease-b,increase-a:increase-b,increase-a:keep-b`
    .
    It's possible to set up to 5 items.
  - `votingAsset` - asset ID which will be used for votes
  - `startHeight`, `endHeight` - voting period in blocks
  - `quorumPercent` - quorum percent in range [1, 99]
- `@Callable(i) func put()`
  - Can be invoked by a voter to put tokens in the contract during voting
  - A voter can invoke the function several times to add more tokens to increase his power
- `@Callable(i) func castVote(selectedOptions: String)`
  - Can be invoked by a voter to cast a vote from predefined options by contract's owner during voting
  - `selectedOptions` - voter choice from predefined options in constructor `availableOptions`
  - It's possible to change a vote by invoking the function again with another options
- `@Callable(i) func withdraw()`
  - Can be invoked by a voter to withdraw tokens from the contract after voting ends

## Example

An example of deploy can be seen in
testnet https://wavesexplorer.com/addresses/3ND4t98zh5UHbMzcG68nRnJb547HLrHvYzz?network=testnet

## Run tests

### Install docker

Link: https://docs.docker.com/engine/install/

### Install dotnet sdk

Link: https://learn.microsoft.com/en-us/dotnet/core/install/

### Run waves private node in docker

Link: https://github.com/wavesplatform/private-node-docker-image

Example:

```bash
docker run --rm -d --name node -p 6869:6869 wavesplatform/waves-private-node
```

### Execute tests

Execute the following command from the project root:

```bash
dotnet test ./tests --nologo -l "console;verbosity=normal"
```