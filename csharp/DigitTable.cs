﻿namespace Ryu
{
    partial class Global // DigitTable
    {
        // A table of all two-digit numbers. This is used to speed up decimal digit
        // generation by copying pairs of digits into the final output.
        static readonly byte[] DIGIT_TABLE = {
          (byte)'0',(byte)'0',(byte)'0',(byte)'1',(byte)'0',(byte)'2',(byte)'0',(byte)'3',(byte)'0',(byte)'4',(byte)'0',(byte)'5',(byte)'0',(byte)'6',(byte)'0',(byte)'7',(byte)'0',(byte)'8',(byte)'0',(byte)'9',
          (byte)'1',(byte)'0',(byte)'1',(byte)'1',(byte)'1',(byte)'2',(byte)'1',(byte)'3',(byte)'1',(byte)'4',(byte)'1',(byte)'5',(byte)'1',(byte)'6',(byte)'1',(byte)'7',(byte)'1',(byte)'8',(byte)'1',(byte)'9',
          (byte)'2',(byte)'0',(byte)'2',(byte)'1',(byte)'2',(byte)'2',(byte)'2',(byte)'3',(byte)'2',(byte)'4',(byte)'2',(byte)'5',(byte)'2',(byte)'6',(byte)'2',(byte)'7',(byte)'2',(byte)'8',(byte)'2',(byte)'9',
          (byte)'3',(byte)'0',(byte)'3',(byte)'1',(byte)'3',(byte)'2',(byte)'3',(byte)'3',(byte)'3',(byte)'4',(byte)'3',(byte)'5',(byte)'3',(byte)'6',(byte)'3',(byte)'7',(byte)'3',(byte)'8',(byte)'3',(byte)'9',
          (byte)'4',(byte)'0',(byte)'4',(byte)'1',(byte)'4',(byte)'2',(byte)'4',(byte)'3',(byte)'4',(byte)'4',(byte)'4',(byte)'5',(byte)'4',(byte)'6',(byte)'4',(byte)'7',(byte)'4',(byte)'8',(byte)'4',(byte)'9',
          (byte)'5',(byte)'0',(byte)'5',(byte)'1',(byte)'5',(byte)'2',(byte)'5',(byte)'3',(byte)'5',(byte)'4',(byte)'5',(byte)'5',(byte)'5',(byte)'6',(byte)'5',(byte)'7',(byte)'5',(byte)'8',(byte)'5',(byte)'9',
          (byte)'6',(byte)'0',(byte)'6',(byte)'1',(byte)'6',(byte)'2',(byte)'6',(byte)'3',(byte)'6',(byte)'4',(byte)'6',(byte)'5',(byte)'6',(byte)'6',(byte)'6',(byte)'7',(byte)'6',(byte)'8',(byte)'6',(byte)'9',
          (byte)'7',(byte)'0',(byte)'7',(byte)'1',(byte)'7',(byte)'2',(byte)'7',(byte)'3',(byte)'7',(byte)'4',(byte)'7',(byte)'5',(byte)'7',(byte)'6',(byte)'7',(byte)'7',(byte)'7',(byte)'8',(byte)'7',(byte)'9',
          (byte)'8',(byte)'0',(byte)'8',(byte)'1',(byte)'8',(byte)'2',(byte)'8',(byte)'3',(byte)'8',(byte)'4',(byte)'8',(byte)'5',(byte)'8',(byte)'6',(byte)'8',(byte)'7',(byte)'8',(byte)'8',(byte)'8',(byte)'9',
          (byte)'9',(byte)'0',(byte)'9',(byte)'1',(byte)'9',(byte)'2',(byte)'9',(byte)'3',(byte)'9',(byte)'4',(byte)'9',(byte)'5',(byte)'9',(byte)'6',(byte)'9',(byte)'7',(byte)'9',(byte)'8',(byte)'9',(byte)'9'
        };
    }
}
