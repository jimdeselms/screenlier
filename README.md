##### Screenlier #

Basic architecture:

1) There's a backing database that stores all the jobs and images
2) There's a service that is called to start jobs, mark their status, and record images
3) Clients submit test data; the main service itself isn't responsible for running the tests; it just collects the data
4) The clients don't check differences. There's a separate process (or lambdas, or agents, or whatever,) that get jobs from the queue and get the differences.
5) Selenium servers - for Chrome and Firefox, these can just be in containers, which means we can run any number of them. For IE, we'll need a small farm of dedicated machines. Just one would be enough for starters.

### API ###

Create a new job
Mark a job complete
Post a test image
Post a reference image

### Client ###
Basically runs a program to do a bunch of Selenium stuff, running against the selenium servers, and posting results to the API

### Differs ###
These run in a look to check for work to do. If they find it, they download it, do the diff, and post the results back up.


## To Do ##

* Don't show tests as successful until their results have come back
~~* There should be a blob table, and images should be referenced by sha. There will be lots and lots of images in this~~ ~~database, and much of it will be duplicate. ~~
    * And there needs to be a method to prune the orphaned blobs
* After a while, test runs that are in a running state for too long should be in an error state, with an indication that it timed out.
    * There are two kinds of timeouts: 
        * Tests that don't submit all of their test images (that is, they never mark the test as done.)
        * Images that aren't differed for a long time
* Add a state for "Ignored", meaning that there was a difference, but you don't care.
* Allow comments when 
    * Upgrading a benchmark
    * Ignoring a failed test result
~~* Show a progress bar of some kind on the summary screen~~
    ~~* It can estimate the number of images based on previous runs~~
    ~~* But in the short term we can just show the status (how many tests have been recorded, how many are still waiting, etc.)~~
    ~~* Actually, I'm going to give this a try, and I don't think that I need to get the estimated number from the database; I already have the image counts~~
        ~~in the summary object itself. neat.~~
* If the differ has an error reading a file, mark the test as an error
    * I think I implemented this, but need to double-check
* When promoting a benchmark, the difference file should be deleted
    * This actually does happen, but isn't being properly cleaned up in the UI
* Automatically refresh pages every few seconds
* Add checkbox for showing or hiding successful tests
* Need an "ignore" button which marks a test as passed, but doesn't replace the bechmark.
* ~~The basic repository and API are implemented.~~
* Need to implement a simple differ; it'll need to use an image library of some kind. It'll basically be in a separate thread (or different process is better.)
* ~~Need to write a simplistic UI.~~
* Update the UI
* Add an image type to the TestImage/Benchmark/DiffImage tables