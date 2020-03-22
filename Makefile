DOTNET=/mnt/c/Program\ Files/dotnet/dotnet.exe
DOTNET_ARGS=-noLogo -clp:NoSummary
SRC_FILES=$(shell find src -name '*.cs')

.PHONY: clean
clean:
	$(DOTNET) clean $(DOTNET_ARGS)
	rm -rf bin

.PHONY: build
build: $(SRC_FILES)
	$(DOTNET) build $(DOTNET_ARGS) -c Debug

.PHONY: run
run: $(SRC_FILES)
	$(DOTNET) run -c Debug -- cfg/config.cs

.PHONY: build-release
build-release: $(SRC_FILES)
	$(DOTNET) build $(DOTNET_ARGS) -c Release

.PHONY: run-release
run-release: $(SRC_FILES)
	$(DOTNET) run -c Release -- cfg/config.cs

.PHONY: run-daemon
run-daemon:
	./scripts/forever '$(DOTNET) run -c Release -- cfg/config.cs'
