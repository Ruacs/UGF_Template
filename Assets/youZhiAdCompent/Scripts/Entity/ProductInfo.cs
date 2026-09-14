namespace YzAdComponent
{

    /// <summary>
    /// 商品信息
    /// ID、名称、价格
    /// </summary>
    public class ProductInfo
    {

        public ProductInfo() { }


        /// <summary>
        /// 商品ID
        /// </summary>
        public string pid;

        /// <summary>
        /// 商品名称
        /// </summary>
        public string name;

        /// <summary>
        /// 价格
        /// </summary>
        public double price;


        /// <summary>
        /// 格式化价格,包含单位
        /// </summary>
        public string formattedPrice;

        /// <summary>
        /// 当前价格货币代码
        /// </summary>
        public string priceCurrencyCode;

        /// <summary>
        /// 价格微单位
        /// </summary>
        public long priceAmountMicros;



        /// <summary>
        /// 商品信息
        /// </summary>
        /// <param name="id">商品ID</param>
        /// <param name="productName">商品名称</param>
        public ProductInfo(string id, string productName)
        {
            this.pid = id;
            this.name = productName;
        }

        /// <summary>
        /// 商品信息
        /// </summary>
        /// <param name="id">商品ID</param>
        /// <param name="productName">商品名称</param>
        /// <param name="formattedPrice">格式化价格</param>
        public ProductInfo(string id, string productName, string formattedPrice)
        {
            this.pid = id;
            this.name = productName;
            this.formattedPrice = formattedPrice;
        }

        public override string ToString()
        {
            return "ProductInfo: #Pid=" + pid + "  #Name=" + name + " #Price=" + price + " #FormattedPrice=" + formattedPrice;
        }
    }

}