using System;
using YangiAlifbo.Core;

var detector = new LanguageDetector();
Console.WriteLine("boshlanadi IsUzbek: " + detector.IsUzbek("boshlanadi"));
Console.WriteLine("boshlanadi ShouldConvert: " + detector.ShouldConvert("boshlanadi"));
Console.WriteLine("boshlanadi Convert: " + UzbekPhonetics.Convert("boshlanadi"));