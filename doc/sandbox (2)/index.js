const express = require("express");
const app = express();
const cors = require("cors");
const PostRouter = require("./routes/PostRouter");
app.use(cors());

const dbConnect = require("./db/dbConnect.js");
dbConnect();
app.use(express.json());
app.use("/api", PostRouter);

app.listen(8080, function () {
  console.log("example on port 8080");
});
