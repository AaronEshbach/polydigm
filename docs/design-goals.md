# Polydigm Framework

The Polydigm Framework is intended as a platform for building modern, service-oriented or microservice applications using the best patterns and practices from different paradigms of programming.

## Design Influences

- Functional Programming
    - Immutable Objects
        - All objects and values should be immutable by default.
        - Mutable state only used in local computations for temporary variables (e.g. an accumulator value within a method).
    - Algebraic Data Types
        - Represent discrete states or conditions with sum types that encapsulate mutually exclusive cases for a single data type (e.g. query parameters for a specific entity).
    - Pipelines and Lambda Functions
        - Fluent-style syntax using lambda expressions to describe algorithms as steps in a pipeline.
    - Composition
        - Object and Function composition to combine small, discrete, re-usable components into larger structures
- Object-Oriented Programming
    - Coding to Interfaces
        - Use of interfaces to abstract contracts between different components (e.g. Service, Repository, Serializer, Protocol, etc.).
    - Dependency Injection
        - Implementation of interfaces and abstractions provided by DI Container
        - All dependencies for a component provided by formal parameters (either constructor parameters or method parameters).
    - Object Inheretance (where preferrable over composition)
        - Class and inteface-based inheritance for situations where it is perferrable to extend an object and add fields or functionality rather than compose two or more objects together.
- Aspect-Oriented Programming
    - Separation of Cross-Cutting Concerns
        - Common features like Caching, Logging/Auditing, Distributed Tracing, and Authorization can be decoupled from the functional implementation and standardized across all services using the framework
    - Annotation via Attributes
        - Metadata attributes used to represent both functional and non-functional aspects of a class or method.
- Domain-Driven Design
    - Alignment of Entities and Models to functional design
    - Isolation of services and entities into bounded contexts
    - Use of events to notify related contexts or services of changes
- Clean Architecture
    - Modular, Decoupled Design with each component abstracted by an interface
        - Allows for implementations to be swapped/replaced without other changes
    - Testability of each component in isolation
        - Mock versions of dependencies can be used to test components independently
- Actor Model
    - Enablement of advanced features like clustering, sharding, and partitioning through the Actor model of concurrency
        - Actors can represent specific instances of entities and enable concurrent processing for requests targeting different instances or entities while ensuring requests for the same instance of an entity are processed in series.
    - Use of Consistent Hashing to Route Requests 
        - Actors can be distributed to different nodes or shards of a cluster and requests can be dynamically routed to the appropriate shard based on a consistent-hashing algorithm.
    
## Framework Goals

- Allow developers to create Robust, Secure, Performant, and Maintainable applications with minimal boilerplate, low barrier to entry, high productivity, and maximum re-usability.
- Decouple non-functional requirements from application functionality so they can be standardized and provided by the framework with little or not effort by the application developer.
- Facilitate the use of different protocols, platforms, and persistence layers interchangeably so that application services are not tightly coupled to a specific technology.
- Utilize the most powerful features of modern languages, from the type systems to source generators, to enable developers to achieve robust implementations of application features with minimal overhead, both during development and at runtime.

## Framework Features

- Metadata Service
    - Application Metadata is available at runtime via an injectable Metadata Service.
    - Can be used to self-host documentation pages or provide details of requirements in error messages.
- Model Validation
    - Utilizes the language Type System to enforce invariants and ensure only valid data is passed to the service layer of an application.
        - Create strong, efficient types for each field, such as utilizing readonly structs in C#, to wrap primitive values and enforce invariants in a private constructor.  
        - Make it "impossible" for an instance of the type to exist with invalid data, so that validation only needs to happen once.
    - Automatically provides detailed errors for any validation failure that occurs.
    - Enables validation to be done in one place (the model implementation) and eliminates the need for validation within the service layer or application logic itself.
- Documentation Generation from Source
    - Support for automatically generating various forms of documentation from the application source code (utilizing the metadata service):
        - JSON Schema
        - OpenAPI Specifications
        - AsyncAPI Specifications
        - WSDL/WADL
- Source Generation from Documentation
    - Support for generating application models and service stubs from supported documentation standards:
        - JSON Schema
        - OpenAPI Specifications
        - AsyncAPI Specifications
        - WSDL/WADL
- Protocol Abstraction
    - Enable the same services to be exposed using different protocols, while enforcing the same security and other non-functional requirements:
        - HTTP/REST
        - gRpc
        - GraphQL
        - SOAP
        - AMQP
        - WebHooks
        - Web Sockets
- Serialization
    - Allow the serialization to be controlled by the protocol or by the client, depending upon the protocol requirements
        - JSON
        - XML
        - BSON
- Auditing and Logging
    - Enable automatic auditing of every request/response 
    - Standardize logging and log aggregation
    - Provide an auditing service and log indexing service to make audits and log data searchable
- Distributed Tracing
    - Emit OpenTelmetry signals to participate in distributed tracing
    - Support W3C traceparent headers and add spans to headers for each service and repository-layer operations
- Caching
    - Enable service-layer and repository-layer operations to be cached
    - Provide central management of the cached artifacts
    - Enable application events to trigger cache expiration/eviction
- Events and Notifications
    - Standardize the way applications raise events or send notifications
    - Decouple the events from a specific protocol such as AMQP or WebSockets
    - Enable applications to consume events or notifications from other applications or services
- Clustering
    - Provide clustering, sharding, and partitioning capabilities when required by a service
    - Enable requests to be routed to the correct node/shard/partition automatically

