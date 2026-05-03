const mongoose = require("mongoose");
const PostSchema = new mongoose.Schema(
  {
    slug: {
      type: String,
      required: [true, "Please provide slug"],
      unique: [true, "Slug Exist"],
    },
    title: {
      type: String,
      required: [true, "Please provide a title!"],
    },
    description: {
      type: String,
      required: [true, "Please provide a description!"],
    },
  },
  { collection: "blogposts" }
);
module.exports =
  mongoose.models.Post || mongoose.model("blogposts", PostSchema);
