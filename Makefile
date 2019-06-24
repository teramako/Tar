TARGET = Debug
MSBUILD_PARAM = -p:Configuration=$(TARGET)

WINTPY  := winpty
MSBUILD := /c/Windows/Microsoft.NET/Framework/v4.0.30319/msbuild.exe

TAR_SRCS := src/*.cs
SAMPLE_SRCS := src/sample/*.cs

.PHONY: build test

 winpty  tar.csproj -t:Build

$(TARGET)/Tar.dll: $(TAR_SRCS)
	$(WINTPY) $(MSBUILD) tar.csproj -t:Build $(MSBUILD_PARAM)

$(TARGET)/Sample.exe: $(SAMPLE_SRCS) $(TARGET)/Tar.dll
	$(WINTPY) $(MSBUILD) tar.csproj -t:BuildSample $(MSBUILD_PARAM)

build: $(TARGET)/Tar.dll

test: $(TARGET)/Sample.exe
	@echo "----------------------------------------------------"
	$(WINTPY) $(TARGET)/Sample.exe test.tar.gz

clean:
	rm -fv Debug/*
	rm -fv Release/*

