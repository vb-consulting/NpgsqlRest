namespace NpgsqlRest;
 
 public interface IEndpointCreateHandler
 {
     void Setup(IApplicationBuilder builder, ILogger? logger) {  }
 
     void Handle(Routine routine, RoutineEndpoint endpoint);
     
     void Cleanup() {  }
 }
 
 public interface IRoutineSource
 {
     IEnumerable<Routine> Read(NpgsqlRestOptions options);
 }