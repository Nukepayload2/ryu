namespace Ryu
{
    partial class Global // DigitTable
    {
        // A table of all two-digit numbers. This is used to speed up decimal digit
        // generation by copying pairs of digits into the final output.
        static readonly sbyte[] DIGIT_TABLE = {
          (sbyte)'0',(sbyte)'0',(sbyte)'0',(sbyte)'1',(sbyte)'0',(sbyte)'2',(sbyte)'0',(sbyte)'3',(sbyte)'0',(sbyte)'4',(sbyte)'0',(sbyte)'5',(sbyte)'0',(sbyte)'6',(sbyte)'0',(sbyte)'7',(sbyte)'0',(sbyte)'8',(sbyte)'0',(sbyte)'9',
          (sbyte)'1',(sbyte)'0',(sbyte)'1',(sbyte)'1',(sbyte)'1',(sbyte)'2',(sbyte)'1',(sbyte)'3',(sbyte)'1',(sbyte)'4',(sbyte)'1',(sbyte)'5',(sbyte)'1',(sbyte)'6',(sbyte)'1',(sbyte)'7',(sbyte)'1',(sbyte)'8',(sbyte)'1',(sbyte)'9',
          (sbyte)'2',(sbyte)'0',(sbyte)'2',(sbyte)'1',(sbyte)'2',(sbyte)'2',(sbyte)'2',(sbyte)'3',(sbyte)'2',(sbyte)'4',(sbyte)'2',(sbyte)'5',(sbyte)'2',(sbyte)'6',(sbyte)'2',(sbyte)'7',(sbyte)'2',(sbyte)'8',(sbyte)'2',(sbyte)'9',
          (sbyte)'3',(sbyte)'0',(sbyte)'3',(sbyte)'1',(sbyte)'3',(sbyte)'2',(sbyte)'3',(sbyte)'3',(sbyte)'3',(sbyte)'4',(sbyte)'3',(sbyte)'5',(sbyte)'3',(sbyte)'6',(sbyte)'3',(sbyte)'7',(sbyte)'3',(sbyte)'8',(sbyte)'3',(sbyte)'9',
          (sbyte)'4',(sbyte)'0',(sbyte)'4',(sbyte)'1',(sbyte)'4',(sbyte)'2',(sbyte)'4',(sbyte)'3',(sbyte)'4',(sbyte)'4',(sbyte)'4',(sbyte)'5',(sbyte)'4',(sbyte)'6',(sbyte)'4',(sbyte)'7',(sbyte)'4',(sbyte)'8',(sbyte)'4',(sbyte)'9',
          (sbyte)'5',(sbyte)'0',(sbyte)'5',(sbyte)'1',(sbyte)'5',(sbyte)'2',(sbyte)'5',(sbyte)'3',(sbyte)'5',(sbyte)'4',(sbyte)'5',(sbyte)'5',(sbyte)'5',(sbyte)'6',(sbyte)'5',(sbyte)'7',(sbyte)'5',(sbyte)'8',(sbyte)'5',(sbyte)'9',
          (sbyte)'6',(sbyte)'0',(sbyte)'6',(sbyte)'1',(sbyte)'6',(sbyte)'2',(sbyte)'6',(sbyte)'3',(sbyte)'6',(sbyte)'4',(sbyte)'6',(sbyte)'5',(sbyte)'6',(sbyte)'6',(sbyte)'6',(sbyte)'7',(sbyte)'6',(sbyte)'8',(sbyte)'6',(sbyte)'9',
          (sbyte)'7',(sbyte)'0',(sbyte)'7',(sbyte)'1',(sbyte)'7',(sbyte)'2',(sbyte)'7',(sbyte)'3',(sbyte)'7',(sbyte)'4',(sbyte)'7',(sbyte)'5',(sbyte)'7',(sbyte)'6',(sbyte)'7',(sbyte)'7',(sbyte)'7',(sbyte)'8',(sbyte)'7',(sbyte)'9',
          (sbyte)'8',(sbyte)'0',(sbyte)'8',(sbyte)'1',(sbyte)'8',(sbyte)'2',(sbyte)'8',(sbyte)'3',(sbyte)'8',(sbyte)'4',(sbyte)'8',(sbyte)'5',(sbyte)'8',(sbyte)'6',(sbyte)'8',(sbyte)'7',(sbyte)'8',(sbyte)'8',(sbyte)'8',(sbyte)'9',
          (sbyte)'9',(sbyte)'0',(sbyte)'9',(sbyte)'1',(sbyte)'9',(sbyte)'2',(sbyte)'9',(sbyte)'3',(sbyte)'9',(sbyte)'4',(sbyte)'9',(sbyte)'5',(sbyte)'9',(sbyte)'6',(sbyte)'9',(sbyte)'7',(sbyte)'9',(sbyte)'8',(sbyte)'9',(sbyte)'9'
        };
    }
}
