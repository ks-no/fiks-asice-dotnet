# fiks-asice
[![MIT license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/ks-no/fiks-asice-dotnet/blob/master/LICENSE)

Library for working with ASiC-E packages in .Net Core based projects. 
This project is created by KS and released under a MIT license.

For more information on how to use this library, check the unit test module. The long term goal of this project is 
to be functionally compliant with the [DIFI ASiC library for Java](//github.com/difi/asic/).

Currently implements:
* building ASiC-E packages containing binaries containing CAdES descriptor
* signing packages using private/public keys in PEM files
* reading ASiC-E packages including exposing CAdES signatures (only CAdES manifest descriptor supported for now)

TODO:
* support for specifying OASIS manifest 
* support XAdES signatures
