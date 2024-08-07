# SyntheSim - Java

This is the SyntheSim Java utility library. FRC users can add this to their project to enhance the simulation capabilities of commonly used libraries

## Current 3rd-Party Support

This is a list of the following 3rd-Party libraries that SyntheSim - Java improves, as well as the level of capability currently offered.

### REVRobotics
- [ ] CANSparkMax
  - [x] Basic motor control
  - [x] Basic internal encoder data
  - [ ] Motor following
  - [ ] Full encoder support

### CTRE Phoenix
- [ ] TalonFX
  - [ ] Basic motor control
  - [ ] Basic internal encoder data
  - [ ] Motor following
  - [ ] Full encoder support

## Building

To build the project, run the `build` task:

<details>
  <summary>Example</summary>

  Windows:
  ```sh
  $ gradlew.bat build
  ```

  MacOS/Linux:
  ```sh
  $ ./gradlew build
  ```
</details>

## Usage

Currently, SyntheSimJava is only available as a local repository. This means it will need to be published and accessed locally.

### Publish (Local)

To publish the project locally, run the `publishToMavenLocal` task:

<details>
  <summary>Example</summary>

  Windows:
  ```sh
  $ gradlew.bat publishToMavenLocal
  ```

  MacOS/Linux:
  ```sh
  $ ./gradlew publishToMavenLocal
  ```
</details>

### Adding to project locally

In order to add the project locally, you must include the the `mavenLocal()` repository to your projects:

```groovy
repositories {
  mavenLocal()
  ...
}
```

Then, add the implementation to your dependencies:

```groovy
dependencies {
  ...
  implementation "com.autodesk.synthesis:SyntheSimJava:1.0.0"
  ...
}
```

### Swapping Imports

SyntheSimJava creates alternative classes that wrap the original ones. Everything that we intercept is passed on to the original class, making it so these classes can (although not recommended) be used when running your robot code on original hardware. Be sure to switch over any and all CAN devices that this project supports in order to effectively simulate your code inside of Synthesis, or with any HALSim, WebSocket supported simulation/device.