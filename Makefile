.PHONY: run watch 

run:
	dotnet run --project src/Gtr $(args)

watch:
	dotnet watch run --project src/Gtr

