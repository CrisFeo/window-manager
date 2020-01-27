DOTNET='/mnt/c/Program Files/dotnet/dotnet.exe'
SRC_FILES=$(shell find src -name '*.cs')

.PHONY: clean
clean:
	$(DOTNET) clean -noLogo -clp:NoSummary

.PHONY: build
build: $(SRC_FILES)
	$(DOTNET) build -noLogo -clp:NoSummary

.PHONY: run
run: $(SRC_FILES)
	$(DOTNET) run cfg/config.cs

.PHONY: run-release
run-release: $(SRC_FILES)
	$(DOTNET) run -c Release
