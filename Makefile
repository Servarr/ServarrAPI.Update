.PHONY: all clean

all:
	dotnet build src/*.sln

clean: 
	rm -rf _artifacts/ _output/ _temp/ _tests/
