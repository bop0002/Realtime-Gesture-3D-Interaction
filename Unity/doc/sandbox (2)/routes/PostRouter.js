const express = require("express");
const Post = require("../db/postModel");
const mongoose = require("mongoose");
const router = express.Router();
const bodyParser = require("body-parser");
const jsonParser = bodyParser.json();

const userSchema = new mongoose.Schema(
  {
    username: String,
    password: String,
  },
  { collection: "users" }
);

const User = mongoose.models.User || mongoose.model("User", userSchema);

router.post("/post", jsonParser, async (request, response) => {
  const post = new Post(request.body);
  try {
    await post.save();
    response.send(post);
  } catch (error) {
    response.status(500).send(error);
  }
});

router.post("/login", jsonParser, async (req, res) => {
  try {
    const { username, password } = req.body;
    // Tìm user có username và password khớp trên Atlas
    const user = await User.findOne({
      username: username,
      password: password,
    });

    if (user) {
      res.status(200).send({ message: "Login successful" });
    } else {
      res
        .status(400)
        .send({ message: "Login failed: Wrong username or password" });
    }
  } catch (err) {
    res.status(500).json({ message: err.message });
  }
});

router.post("/register", jsonParser, async (req, res) => {
  const { username, password } = req.body;

  try {
    const user = new User({ username, password });
    await user.save();

    res.status(200).json("Đăng ký thành công");
  } catch (err) {
    res.status(500).json("Lỗi server");
  }
});

router.get("/posts", async (request, response) => {
  try {
    const posts = await Post.find({});
    response.send(posts);
  } catch (error) {
    response.status(500).send({ error });
  }
});
router.get("/users", async (resquest, response) => {
  try {
    const users = await User.find({});
    response.send(users);
  } catch (error) {
    response.status(500).send({ error });
  }
});

router.get("/post/:slug", async (request, response) => {
  try {
    const post = await Post.findOne({ slug: request.params.slug });
    response.send(post);
  } catch (error) {
    response.status(500).send({ error });
  }
});

router.patch("/post/:slug", async (request, response) => {
  try {
    const post = await Post.findOneAndUpdate(
      { slug: request.params.slug },
      request.body
    );
    response.status(200).send(post);
  } catch (error) {
    response.status(500).send({ error });
  }
});

router.delete("/post/:slug", async (request, response) => {
  try {
    const post = await Post.findOneAndDelete({ slug: request.params.slug });
    if (!post) {
      return response.status(404).send("Post wasn't found");
    }
    response.status(204).send("delete successfully");
  } catch (error) {
    response.status(500).send({ error });
  }
});

module.exports = router;
