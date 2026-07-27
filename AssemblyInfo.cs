using System.Runtime.CompilerServices;

// The scanner's pure logic (icon header parsing, tag decisions, settings projections)
// is internal rather than public. Exposing it to the test assembly lets those tests
// call it directly instead of reaching in through reflection.
[assembly: InternalsVisibleTo("MediaAudit.Tests")]
