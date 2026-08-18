.PHONY: run watch 

run:
	dotnet run --project src/Gtr -- $(ARGS)

watch:
	dotnet watch run --project src/Gtr

