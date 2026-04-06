[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
// Each test method gets its own VideoStore/UserStore instances; parallel is safe and shortens CI.
