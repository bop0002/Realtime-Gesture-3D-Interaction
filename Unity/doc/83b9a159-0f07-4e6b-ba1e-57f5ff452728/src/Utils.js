export function UpdatePost() {
  const { slug } = useParams();
  const [res, setRes] = useState("");
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm();
  const onSubmit = async (data) => {
    const post = JSON.stringify(data);
    try {
      const response = await fetch(
        "https://nknd9k-8080.csb.app/api/post/" + slug,
        {
          method: "PATCH",
          headers: {
            Accept: "application /json",
            "Content-Type": "application/json",
          },
          body: post,
        }
      );
      if (response.ok) setRes("Post updated successfully!");
    } catch (error) {
      console.error("Error updated data:", error);
      setRes("Post updated failed!");
    }
  };
  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <div style={{ padding: 10 }}>
        <br />
        <span>Slug:</span>
        <br />
        <input type="text" {...register("slug", { required: true })} />
        <br />
        {errors.slug && <div style={{ color: "red" }}>Slug is required</div>}
        <span>Title:</span>
        <br />
        <input type="text" {...register("title", { required: true })} />
        <br />
        {errors.title && <div style={{ color: "red" }}>Title is required</div>}
        <span>Description:</span>
        <br />
        <input type="text" {...register("description", { required: true })} />
        <br />
        {errors.description && (
          <div style={{ color: "red" }}>Description is required</div>
        )}
        <br />
        <button type="submit">update post</button>
        <p className="text-success">{res}</p>
      </div>
    </form>
  );
}

const handleUpdate = () => {
  navigate(`/posts/update/${slug}`);
};

const handleDelete = async () => {
  try {
    const response = await fetch(
      "https://nknd9k-8080.csb.app/api/post/" + slug,
      {
        method: "Delete",
        headers: {
          Accept: "application /json",
          "Content-Type": "application/json",
        },
      }
    );
    if (response.ok) {
      navigate("/posts");
    }
  } catch (error) {
    console.error("Error deleting data:", error);
  }
};
