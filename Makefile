SRC_FILES=$(shell find src -name *.cs)

.PHONY: clean
clean:
	dotnet.exe clean -noLogo -clp:NoSummary

.PHONY: build
build: $(SRC_FILES)
	dotnet.exe build -noLogo -clp:NoSummary

.PHONY: run
run: $(SRC_FILES)
	dotnet.exe run
